using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillUpAPI.Domain.Entities;
using SkillUpAPI.DTOs;
using SkillUpAPI.DTOs.CourseDTOs;
using SkillUpAPI.DTOs.LessonDTOs;
using SkillUpAPI.Persistence;
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

        public CoursesController(AppDbContext db)
        {
            _db = db;
        }

        // Helper to get user id from JWT (sub claim)
        private int? GetUserId()
        {
            var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(sub)) return null;
            return int.TryParse(sub, out var id) ? id : (int?)null;
        }

        // Public listing with optional filters & pagination
        // GET: /api/courses?categoryId=1&q=csharp&page=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? categoryId, [FromQuery] string? q,
           [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var query = _db.Courses
                .AsNoTracking()
                .Include(c => c.Category)
                .Include(c => c.Teacher)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(c => c.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                query = query.Where(c => c.Title.ToLower().Contains(q) || (c.Description ?? "").ToLower().Contains(q));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CourseListDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    CategoryId = c.CategoryId,
                    CategoryName = c.Category != null ? c.Category.Name : null,
                    TeacherId = c.TeacherId,
                    TeacherUsername = c.Teacher.Username,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            var result = new PagedResult<CourseListDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Items = items
            };

            return Ok(result);
        }
        // GET /api/courses/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var course = await _db.Courses
                .AsNoTracking()
                .Include(c => c.Category)
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            var dto = new CourseDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                CategoryId = course.CategoryId,
                CategoryName = course.Category?.Name,
                TeacherId = course.TeacherId,
                TeacherUsername = course.Teacher.Username,
                CreatedAt = course.CreatedAt
            };

            return Ok(dto);
        }

        // GET /api/courses/{id}/lessons
        // Only teacher (owner) / admin / enrolled students can view lessons
        [HttpGet("{id:int}/lessons")]
        [Authorize] // must be authenticated
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
                .AsNoTracking()
                .Where(l => l.CourseId == id)
                .OrderBy(l => l.OrderIndex)
                .Select(l => new LessonDto
                {
                    Id = l.Id,
                    CourseId = l.CourseId,
                    Title = l.Title,
                    ContentUrl = l.ContentUrl,
                    OrderIndex = l.OrderIndex
                })
                .ToListAsync();

            return Ok(lessons);
        }

        // Create course
        // POST /api/courses
        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Create([FromBody] CourseCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            // If teacher role, teacherId must be this user
            if (User.IsInRole(nameof(UserRole.Teacher)))
            {
                dto.TeacherId = userId.Value;
            }
            else if (User.IsInRole(nameof(UserRole.Admin)))
            {
                // Admin can create and assign teacher; if not provided, set admin as teacher
                if (!dto.TeacherId.HasValue) dto.TeacherId = userId.Value;
            }

            // Validate teacher exists
            var teacher = await _db.Users.FirstOrDefaultAsync(u => u.Id == dto.TeacherId.Value && u.Role == UserRole.Teacher);
            if (teacher == null) return BadRequest(new { error = "Assigned teacher not found or not a teacher." });

            var course = new Course
            {
                Title = dto.Title,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                TeacherId = dto.TeacherId.Value,
                CreatedAt = DateTime.UtcNow
            };

            _db.Courses.Add(course);
            await _db.SaveChangesAsync();

            var createdDto = new CourseDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                CategoryId = course.CategoryId,
                CategoryName = (await _db.Categories.FindAsync(course.CategoryId))?.Name,
                TeacherId = course.TeacherId,
                TeacherUsername = teacher.Username,
                CreatedAt = course.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = course.Id }, createdDto);
        }

        // Update course
        // PUT /api/courses/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CourseUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var course = await _db.Courses.FindAsync(id);
            if (course == null) return NotFound();

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var isAdmin = User.IsInRole(nameof(UserRole.Admin));
            var isTeacherOwner = User.IsInRole(nameof(UserRole.Teacher)) && course.TeacherId == userId;

            if (!isAdmin && !isTeacherOwner)
                return Forbid();

            // Only admin can change TeacherId
            if (dto.TeacherId.HasValue && isAdmin)
            {
                var teacher = await _db.Users.FirstOrDefaultAsync(u => u.Id == dto.TeacherId.Value && u.Role == UserRole.Teacher);
                if (teacher == null) return BadRequest(new { error = "Assigned teacher not found or not a teacher." });
                course.TeacherId = dto.TeacherId.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.Title)) course.Title = dto.Title;
            course.Description = dto.Description; // allow null to clear
            course.CategoryId = dto.CategoryId;

            _db.Courses.Update(course);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // Delete course
        // DELETE /api/courses/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var course = await _db.Courses.FindAsync(id);
            if (course == null) return NotFound();

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var isAdmin = User.IsInRole(nameof(UserRole.Admin));
            var isTeacherOwner = User.IsInRole(nameof(UserRole.Teacher)) && course.TeacherId == userId;

            if (!isAdmin && !isTeacherOwner)
                return Forbid();

            _db.Courses.Remove(course);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // Enroll current user to a course
        // POST /api/courses/{id}/enroll
        [HttpPost("{id:int}/enroll")]
        [Authorize(Roles = "Student,Teacher,Admin")] // allow teachers/admins to enroll themselves if desired
        public async Task<IActionResult> Enroll(int id)
        {
            var course = await _db.Courses.FindAsync(id);
            if (course == null) return NotFound();

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var exists = await _db.Enrollments.AnyAsync(e => e.CourseId == id && e.UserId == userId);
            if (exists) return BadRequest(new { error = "Already enrolled." });

            var enrollment = new Enrollment
            {
                CourseId = id,
                UserId = userId.Value,
                EnrolledAt = DateTime.UtcNow
            };

            _db.Enrollments.Add(enrollment);
            await _db.SaveChangesAsync();

            return StatusCode(201); // Created - no body
        }

        // GET /api/courses/my
        // Returns courses the current user is enrolled in OR teaches
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyCourses()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            // courses where user is teacher
            var teaching = await _db.Courses
                .AsNoTracking()
                .Where(c => c.TeacherId == userId)
                .Select(c => new CourseListDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    CategoryId = c.CategoryId,
                    CategoryName = c.Category != null ? c.Category.Name : null,
                    TeacherId = c.TeacherId,
                    TeacherUsername = c.Teacher.Username,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            // courses where user is enrolled
            var enrolled = await _db.Enrollments
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Teacher)
                .Select(e => new CourseListDto
                {
                    Id = e.Course.Id,
                    Title = e.Course.Title,
                    Description = e.Course.Description,
                    CategoryId = e.Course.CategoryId,
                    CategoryName = e.Course.Category != null ? e.Course.Category.Name : null,
                    TeacherId = e.Course.TeacherId,
                    TeacherUsername = e.Course.Teacher.Username,
                    CreatedAt = e.Course.CreatedAt
                })
                .ToListAsync();

            // Merge (avoid duplicates)
            var merged = teaching.Concat(enrolled).GroupBy(c => c.Id).Select(g => g.First()).ToList();

            return Ok(merged);
        }
    }
}
