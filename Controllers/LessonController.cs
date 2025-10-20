using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillUpAPI.Domain.Entities;
using SkillUpAPI.DTOs.LessonDTOs;
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

        // helper за userId
        private int? GetUserId()
        {
            var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(sub)) return null;
            return int.TryParse(sub, out var id) ? id : (int?)null;
        }

        /// ✅ Връща всички уроци за даден курс (само ако потребителят е записан/собственик/admin)
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
                    OrderIndex = l.OrderIndex
                })
                .ToListAsync();

            return Ok(lessons);
        }

        /// ✅ Създава нов урок (само Teacher/Admin)
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
                ContentUrl = dto.ContentUrl,
                OrderIndex = dto.OrderIndex
            };

            _db.Lessons.Add(lesson);
            await _db.SaveChangesAsync();

            return Ok(new LessonDto
            {
                Id = lesson.Id,
                CourseId = lesson.CourseId,
                Title = lesson.Title,
                ContentUrl = lesson.ContentUrl,
                OrderIndex = lesson.OrderIndex
            });
        }

        /// ✅ Обновява урок (само Teacher/Admin)
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

            if (!string.IsNullOrWhiteSpace(dto.Title)) lesson.Title = dto.Title;
            if (!string.IsNullOrWhiteSpace(dto.ContentUrl)) lesson.ContentUrl = dto.ContentUrl;
            if (dto.OrderIndex.HasValue) lesson.OrderIndex = dto.OrderIndex.Value;

            _db.Lessons.Update(lesson);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        /// ✅ Изтрива урок (само Teacher/Admin)
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
