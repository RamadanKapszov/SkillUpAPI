using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillUpAPI.Domain.Entities;
using SkillUpAPI.DTOs;
using SkillUpAPI.DTOs.CourseDTOs;
using SkillUpAPI.DTOs.LessonDTOs;
using SkillUpAPI.Persistence;
using SkillUpAPI.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SkillUpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CoursesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IProgressService _progressService;

        public CoursesController(AppDbContext db, IProgressService progressService)
        {
            _db = db;
            _progressService = progressService;
        }

        private int? GetUserId()
        {
            var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(sub)) return null;
            return int.TryParse(sub, out var id) ? id : (int?)null;
        }

        // 🔹 PUBLIC COURSE LIST (search & pagination)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? categoryId,
            [FromQuery] string? q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var query = _db.Courses
                .Include(c => c.Category)
                .Include(c => c.Teacher)
                .AsNoTracking()
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(c => c.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(c =>
                    c.Title.ToLower().Contains(term) ||
                    (c.Description ?? "").ToLower().Contains(term));
            }

            var total = await query.CountAsync();


            var courses = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
               .Select(c => new CourseListDto
               {
                   Id = c.Id,
                   Title = c.Title,
                   Description = c.Description,
                   ShortDescription = c.ShortDescription,
                   Level = c.Level,
                   Duration = c.Duration,
                   Language = c.Language,
                   Rating = c.Rating,
                   StudentsCount = c.StudentsCount,
                   CategoryId = c.CategoryId,
                   CategoryName = c.Category != null ? c.Category.Name : null,
                   TeacherId = c.TeacherId,
                   TeacherUsername = c.Teacher.Username,
                   CreatedAt = c.CreatedAt,
                   ImageUrl = c.ImageUrl
               })
                .ToListAsync();

            return Ok(new PagedResult<CourseListDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Items = courses
            });
        }

        // 🔹 GET COURSE DETAILS
        [HttpGet("{id:int}")]
        [Authorize] // so we can detect user
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();
            var course = await _db.Courses
                .Include(c => c.Category)
                .Include(c => c.Teacher)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            var avgRating = await _db.LessonReviews
                .Where(r => r.Lesson.CourseId == id)
                .AverageAsync(r => (double?)r.Rating) ?? 0.0;

            bool isEnrolled = false;
            if (userId.HasValue)
                isEnrolled = await _db.Enrollments.AnyAsync(e => e.CourseId == id && e.UserId == userId.Value);

            var studentsCount = await _db.Enrollments.CountAsync(e => e.CourseId == id);


            return Ok(new
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                ShortDescription = course.ShortDescription,
                Level = course.Level,
                Duration = course.Duration,
                Language = course.Language,
                Prerequisites = course.Prerequisites,
                WhatYouWillLearn = course.WhatYouWillLearn,
                WhoIsFor = course.WhoIsFor,
                Tags = course.Tags,
                Rating = course.Rating,
                StudentsCount = studentsCount,
                CategoryId = course.CategoryId,
                CategoryName = course.Category?.Name,
                TeacherId = course.TeacherId,
                TeacherUsername = course.Teacher.Username,
                CreatedAt = course.CreatedAt,
                AverageRating = Math.Round(avgRating, 1),
                IsEnrolled = isEnrolled,
                ImageUrl = course.ImageUrl
            });

        }


        // 🔹 GET LESSONS FOR A COURSE (requires access)
        [HttpGet("{id:int}/lessons")]
        public async Task<IActionResult> GetLessons(int id)
        {
            var course = await _db.Courses.FindAsync(id);
            if (course == null) return NotFound();

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var isAdmin = User.IsInRole(nameof(UserRole.Admin));
            var isTeacherOwner = User.IsInRole(nameof(UserRole.Teacher)) && course.TeacherId == userId;
            var isEnrolled = await _db.Enrollments.AnyAsync(e => e.CourseId == id && e.UserId == userId);

            if (!isAdmin && !isTeacherOwner && !isEnrolled)
                return Forbid();

            var lessons = await _db.Lessons
                .Where(l => l.CourseId == id)
                .OrderBy(l => l.OrderIndex)
                .Select(l => new LessonDto
                {
                    Id = l.Id,
                    CourseId = l.CourseId,
                    Title = l.Title,
                    Description = l.Description,
                    ContentUrl = l.ContentUrl,
                    OrderIndex = l.OrderIndex
                })
                .ToListAsync();

            return Ok(lessons);
        }



        // 🔹 CREATE COURSE
        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Create([FromBody] CourseCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            if (User.IsInRole(nameof(UserRole.Teacher)))
                dto.TeacherId = userId.Value;
            else if (User.IsInRole(nameof(UserRole.Admin)) && !dto.TeacherId.HasValue)
                dto.TeacherId = userId.Value;

            var teacher = await _db.Users
                .FirstOrDefaultAsync(u => u.Id == dto.TeacherId && u.Role == UserRole.Teacher);
            if (teacher == null)
                return BadRequest(new { error = "Assigned teacher not found or not a teacher." });

            var course = new Course
            {
                Title = dto.Title,
                Description = dto.Description,
                ShortDescription = dto.ShortDescription,
                Level = dto.Level,
                Duration = dto.Duration,
                Language = dto.Language,
                Prerequisites = dto.Prerequisites,
                WhatYouWillLearn = dto.WhatYouWillLearn,
                WhoIsFor = dto.WhoIsFor,
                Tags = dto.Tags,
                Rating = dto.Rating,
                StudentsCount = dto.StudentsCount,
                CategoryId = dto.CategoryId,
                TeacherId = dto.TeacherId.Value,
                CreatedAt = DateTime.UtcNow,
                ImageUrl = dto.ImageUrl
            };


            _db.Courses.Add(course);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = course.Id }, new CourseDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                CategoryId = course.CategoryId,
                TeacherId = course.TeacherId,
                TeacherUsername = teacher.Username,
                CreatedAt = course.CreatedAt
            });
        }

        // 🔹 UPDATE COURSE
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CourseUpdateDto dto)
        {
            var course = await _db.Courses.FindAsync(id);
            if (course == null) return NotFound();

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var isAdmin = User.IsInRole(nameof(UserRole.Admin));
            var isOwner = User.IsInRole(nameof(UserRole.Teacher)) && course.TeacherId == userId;

            if (!isAdmin && !isOwner)
                return Forbid();

            if (dto.TeacherId.HasValue && isAdmin)
            {
                var teacher = await _db.Users.FirstOrDefaultAsync(u => u.Id == dto.TeacherId.Value && u.Role == UserRole.Teacher);
                if (teacher == null) return BadRequest(new { error = "Assigned teacher not found or not a teacher." });
                course.TeacherId = dto.TeacherId.Value;
            }

            course.Title = dto.Title ?? course.Title;
            course.Description = dto.Description ?? course.Description;
            course.CategoryId = dto.CategoryId ?? course.CategoryId;
            course.ShortDescription = dto.ShortDescription ?? course.ShortDescription;
            course.Level = dto.Level ?? course.Level;
            course.Duration = dto.Duration ?? course.Duration;
            course.Language = dto.Language ?? course.Language;
            course.Prerequisites = dto.Prerequisites ?? course.Prerequisites;
            course.WhatYouWillLearn = dto.WhatYouWillLearn ?? course.WhatYouWillLearn;
            course.WhoIsFor = dto.WhoIsFor ?? course.WhoIsFor;
            course.Tags = dto.Tags ?? course.Tags;
            course.Rating = dto.Rating ?? course.Rating;
            course.StudentsCount = dto.StudentsCount ?? course.StudentsCount;
            course.ImageUrl = dto.ImageUrl ?? course.ImageUrl;

            _db.Courses.Update(course);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // 🔹 DELETE COURSE
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var course = await _db.Courses.FindAsync(id);
            if (course == null) return NotFound();

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var isAdmin = User.IsInRole(nameof(UserRole.Admin));
            var isOwner = User.IsInRole(nameof(UserRole.Teacher)) && course.TeacherId == userId;

            if (!isAdmin && !isOwner)
                return Forbid();

            _db.Courses.Remove(course);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // 🔹 ENROLL
        [HttpPost("{id:int}/enroll")]
        [Authorize(Roles = "Student,Teacher,Admin")]
        public async Task<IActionResult> Enroll(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var course = await _db.Courses.Include(c => c.Teacher).FirstOrDefaultAsync(c => c.Id == id);
            if (course == null) return NotFound();

            if (course.TeacherId == userId)
                return BadRequest(new { error = "Teachers cannot enroll in their own course." });

            var exists = await _db.Enrollments.AnyAsync(e => e.CourseId == id && e.UserId == userId);
            if (exists) return BadRequest(new { error = "Already enrolled in this course." });


            var enrollment = new Enrollment
            {
                CourseId = id,
                UserId = userId.Value,
                EnrolledAt = DateTime.UtcNow
            };

            _db.Enrollments.Add(enrollment);
            await _db.SaveChangesAsync();

            // 🎯 Add points + check badges
            await _progressService.AddPointsAsync(userId.Value, 10);
            await _progressService.CheckForNewBadgesAsync(userId.Value);

            return Ok(new { message = "Enrolled successfully." });
        }

        // 🔹 MY COURSES (teaching or enrolled)
        [HttpGet("my")]
        public async Task<IActionResult> GetMyCourses()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            // 🧑‍🏫 Get courses this teacher created
            var teachingCourses = await _db.Courses
                .Where(c => c.TeacherId == userId)
                .Include(c => c.Category)
                .Include(c => c.Teacher)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.ShortDescription,
                    c.Level,
                    c.Duration,
                    c.Language,
                    c.ImageUrl,
                    c.CategoryId,
                    CategoryName = c.Category != null ? c.Category.Name : null,
                    c.TeacherId,
                    TeacherUsername = c.Teacher.Username,
                    c.CreatedAt
                })
                .ToListAsync();

            var result = new List<object>();

            foreach (var c in teachingCourses)
            {
                // 🧮 Count enrolled students
                var studentsCount = await _db.Enrollments.CountAsync(e => e.CourseId == c.Id);

                // ⭐ Compute average rating
                var avgRating = await _db.LessonReviews
                    .Where(r => r.Lesson.CourseId == c.Id)
                    .AverageAsync(r => (double?)r.Rating) ?? 0.0;

                result.Add(new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.ShortDescription,
                    c.Level,
                    c.Duration,
                    c.Language,
                    c.ImageUrl,
                    c.CategoryId,
                    c.CategoryName,
                    c.TeacherId,
                    c.TeacherUsername,
                    c.CreatedAt,
                    StudentsCount = studentsCount,
                    AverageRating = Math.Round(avgRating, 1)
                });
            }

            return Ok(result);
        }


        // 🔹 COURSE PROGRESS
        [HttpGet("{courseId}/progress")]
        public async Task<IActionResult> GetCourseProgress(int courseId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var progress = await _progressService.GetUserCourseProgressAsync(userId.Value, courseId);
            if (progress == null) return NotFound();

            return Ok(progress);
        }

        // 🔹 COMPLETED LESSONS (ids)
        [HttpGet("{courseId}/completed-lessons")]
        public async Task<IActionResult> GetCompletedLessons(int courseId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var completed = await _progressService.GetCompletedLessonsAsync(userId.Value, courseId);
            return Ok(completed);
        }
    }
}
