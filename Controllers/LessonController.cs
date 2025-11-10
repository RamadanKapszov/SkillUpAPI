using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillUpAPI.Domain.Entities;
using SkillUpAPI.DTOs.LessonDTOs;
using SkillUpAPI.DTOs.ReviewDTOs;
using SkillUpAPI.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SkillUpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LessonsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public LessonsController(AppDbContext db)
        {
            _db = db;
        }

        // 🔹 helper for userId
        private int? GetUserId()
        {
            var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(sub)) return null;
            return int.TryParse(sub, out var id) ? id : (int?)null;
        }

        // ✅ Get lesson details (with teacher, avg rating, and test link)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var lesson = await _db.Lessons
                .Include(l => l.Course)
                    .ThenInclude(c => c.Teacher)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
                return NotFound();

            var reviews = await _db.LessonReviews
                .Where(r => r.LessonId == id)
                .Include(r => r.Student)
                .ToListAsync();

            var avgRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;

            var test = await _db.Tests
                .Where(t => t.LessonId == id)
                .Select(t => new { t.Id, t.Title, t.MaxPoints })
                .FirstOrDefaultAsync();

            return Ok(new
            {
                lesson.Id,
                lesson.Title,
                lesson.Description,
                lesson.ContentUrl,
                lesson.Duration,
                lesson.PreviewImageUrl,
                lesson.CourseId,
                TeacherUsername = lesson.Course?.Teacher?.Username,
                TeacherAvatar = lesson.Course?.Teacher?.AvatarUrl,
                AverageRating = avgRating,
                Test = test,
                Reviews = reviews.Select(r => new
                {
                    r.Id,
                    StudentName = r.Student.Username,
                    r.StudentId,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt
                })
            });
        }

        // ✅ Add review (students only)
        [HttpPost("{lessonId}/reviews")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> AddReview(int lessonId, [FromBody] ReviewCreateDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest(new { error = "Rating must be between 1 and 5." });

            var lesson = await _db.Lessons.FindAsync(lessonId);
            if (lesson == null) return NotFound();

            var alreadyReviewed = await _db.LessonReviews
                .AnyAsync(r => r.LessonId == lessonId && r.StudentId == userId);

            if (alreadyReviewed)
                return BadRequest(new { error = "You already reviewed this lesson." });

            var review = new LessonReview
            {
                LessonId = lessonId,
                StudentId = userId.Value,
                Rating = dto.Rating,
                Comment = dto.Comment
            };

            _db.LessonReviews.Add(review);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Review added successfully.",
                review.Id,
                review.Rating,
                review.Comment
            });
        }

        // ✅ Get lessons for a course
        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetLessonsByCourse(int courseId)
        {
            var course = await _db.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var isAdmin = User.IsInRole(nameof(UserRole.Admin));
            var isTeacherOwner = User.IsInRole(nameof(UserRole.Teacher)) && course.TeacherId == userId;
            var isEnrolled = await _db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == userId);

            //if (!isAdmin && !isTeacherOwner && !isEnrolled)
            //    return Forbid();

            var lessons = await _db.Lessons
                .AsNoTracking()
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.OrderIndex)
                .Select(l => new LessonDto
                {
                    Id = l.Id,
                    CourseId = l.CourseId,
                    Title = l.Title,
                    ContentUrl = l.ContentUrl,
                    OrderIndex = l.OrderIndex,
                    Description = l.Description,
                    Duration = l.Duration
                })
                .ToListAsync();

            return Ok(lessons);
        }

        // ✅ Create new lesson (Teacher/Admin)
        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Create([FromBody] LessonCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var course = await _db.Courses.FindAsync(dto.CourseId);
            if (course == null) return NotFound(new { error = "Course not found" });

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var isAdmin = User.IsInRole(nameof(UserRole.Admin));
            var isTeacherOwner = User.IsInRole(nameof(UserRole.Teacher)) && course.TeacherId == userId;

            if (!isAdmin && !isTeacherOwner)
                return Forbid();

            var lesson = new Lesson
            {
                CourseId = dto.CourseId,
                Title = dto.Title,
                Description = dto.Description,
                ContentUrl = dto.ContentUrl,
                Duration = dto.Duration,
                OrderIndex = dto.OrderIndex
            };

            _db.Lessons.Add(lesson);
            await _db.SaveChangesAsync();

            return Ok(new LessonDto
            {
                Id = lesson.Id,
                CourseId = lesson.CourseId,
                Title = lesson.Title,
                Description = lesson.Description,
                ContentUrl = lesson.ContentUrl,
                OrderIndex = lesson.OrderIndex,
                Duration = lesson.Duration
            });
        }

        // ✅ Update lesson
        [HttpPut("{id}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] LessonUpdateDto dto)
        {
            var lesson = await _db.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == id);
            if (lesson == null) return NotFound();

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var isAdmin = User.IsInRole(nameof(UserRole.Admin));
            var isTeacherOwner = User.IsInRole(nameof(UserRole.Teacher)) && lesson.Course.TeacherId == userId;

            if (!isAdmin && !isTeacherOwner)
                return Forbid();

            lesson.Title = dto.Title ?? lesson.Title;
            lesson.Description = dto.Description ?? lesson.Description;
            lesson.ContentUrl = dto.ContentUrl ?? lesson.ContentUrl;
            lesson.Duration = dto.Duration ?? lesson.Duration;
            lesson.OrderIndex = dto.OrderIndex ?? lesson.OrderIndex;

            _db.Lessons.Update(lesson);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // ✅ Delete lesson
        [HttpDelete("{id}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var lesson = await _db.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == id);
            if (lesson == null) return NotFound();

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var isAdmin = User.IsInRole(nameof(UserRole.Admin));
            var isTeacherOwner = User.IsInRole(nameof(UserRole.Teacher)) && lesson.Course.TeacherId == userId;

            if (!isAdmin && !isTeacherOwner)
                return Forbid();

            _db.Lessons.Remove(lesson);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
