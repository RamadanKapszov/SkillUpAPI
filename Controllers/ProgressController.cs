using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillUpAPI.Domain.Entities;
using SkillUpAPI.DTOs.ProgressDtos;
using SkillUpAPI.Persistence;
using SkillUpAPI.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SkillUpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProgressController : ControllerBase
    {
        private readonly IProgressService _progressService;
        private readonly AppDbContext _db;

        public ProgressController(IProgressService progressService, AppDbContext db)
        {
            _progressService = progressService;
            _db = db;
        }

        private int? GetUserId()
        {
            var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(sub)) return null;
            return int.TryParse(sub, out var id) ? id : (int?)null;
        }

        // ✅ Mark lesson as completed
        [HttpPost("lessons/{lessonId}/complete")]
        public async Task<IActionResult> CompleteLesson(int lessonId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var success = await _progressService.MarkLessonCompletedAsync(userId.Value, lessonId);
            if (!success) return BadRequest(new { error = "Lesson already completed" });

            // 🎖 Award points & badges
            var lesson = await _db.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == lessonId);
            var user = await _db.Users.FindAsync(userId);
            if (lesson != null && user != null)
            {
                user.TotalPoints += 10; // +10 points per completed lesson
                await CheckForNewBadgesAsync(userId.Value);
                await _db.SaveChangesAsync();
            }

            // Optionally return next lesson
            var nextLesson = await _db.Lessons
                .Where(l => l.CourseId == lesson!.CourseId && l.OrderIndex > lesson.OrderIndex)
                .OrderBy(l => l.OrderIndex)
                .Select(l => new { l.Id, l.Title })
                .FirstOrDefaultAsync();

            return Ok(new
            {
                message = "Lesson completed successfully",
                nextLesson
            });
        }

        // ✅ Check if specific lesson is completed
        [HttpGet("lessons/{lessonId}/status")]
        public async Task<IActionResult> GetLessonStatus(int lessonId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            bool completed = await _db.LessonCompletions
                .AnyAsync(lc => lc.UserId == userId && lc.LessonId == lessonId);

            return Ok(new { isCompleted = completed });
        }


        // ✅ Get user total points
        [HttpGet("points")]
        public async Task<IActionResult> GetPoints()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var user = await _db.Users.FindAsync(userId.Value);
            if (user == null) return NotFound();

            return Ok(new
            {
                totalPoints = user.TotalPoints
            });
        }

        // ✅ Get user badges
        [HttpGet("badges")]
        public async Task<IActionResult> GetBadges()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var badges = await _progressService.GetUserBadgesAsync(userId.Value);

            var result = badges.Select(b => new
            {
                b.Id,
                b.Name,
                b.Description,
                b.IconUrl,
                b.ConditionType,
                b.Threshold
            });

            return Ok(result);
        }

        // ✅ Get user progress for a course
        [HttpGet("courses/{courseId}/progress")]
        public async Task<IActionResult> GetCourseProgress(int courseId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var course = await _db.Courses
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null) return NotFound();

            var totalLessons = course.Lessons.Count;
            var completedLessons = await _db.LessonCompletions
                .CountAsync(lc => lc.UserId == userId.Value && lc.Lesson.CourseId == courseId);

            var percent = totalLessons > 0 ? Math.Round((double)completedLessons / totalLessons * 100, 1) : 0;

            return Ok(new
            {
                course.Id,
                course.Title,
                totalLessons,
                completedLessons,
                percentCompleted = percent
            });
        }

        // 🎖 Badge logic (shared with TestsController)
        private async Task CheckForNewBadgesAsync(int userId)
        {
            var user = await _db.Users
                .Include(u => u.UserBadges)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return;

            var badges = await _db.Badges.ToListAsync();
            var now = DateTime.UtcNow;

            foreach (var badge in badges)
            {
                bool alreadyEarned = user.UserBadges.Any(b => b.BadgeId == badge.Id);
                if (alreadyEarned) continue;

                switch (badge.ConditionType)
                {
                    case nameof(BadgeCondition.TotalPoints):
                        if (user.TotalPoints >= badge.Threshold)
                            _db.UserBadges.Add(new UserBadge { UserId = userId, BadgeId = badge.Id, AwardedAt = now });
                        break;
                    case nameof(BadgeCondition.CourseCompleted):
                        var coursesCompleted = await _db.Courses
                            .Include(c => c.Lessons)
                            .ToListAsync();
                        var completedCourses = coursesCompleted.Count(c =>
                            c.Lessons.All(l =>
                                _db.LessonCompletions.Any(lc => lc.UserId == userId && lc.LessonId == l.Id)));
                        if (completedCourses >= badge.Threshold)
                            _db.UserBadges.Add(new UserBadge { UserId = userId, BadgeId = badge.Id, AwardedAt = now });
                        break;
                    case nameof(BadgeCondition.TestsCompleted):
                        var testCount = await _db.UserTests.CountAsync(ut => ut.UserId == userId);
                        if (testCount >= badge.Threshold)
                            _db.UserBadges.Add(new UserBadge { UserId = userId, BadgeId = badge.Id, AwardedAt = now });
                        break;
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}
