using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillUpAPI.Domain.Entities;
using SkillUpAPI.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SkillUpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TestsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TestsController(AppDbContext db)
        {
            _db = db;
        }

        private int? GetUserId()
        {
            var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(sub)) return null;
            return int.TryParse(sub, out var id) ? id : (int?)null;
        }

        // ✅ Get test by lesson
        [HttpGet("lesson/{lessonId}")]
        public async Task<IActionResult> GetByLesson(int lessonId)
        {
            var test = await _db.Tests
                .Include(t => t.Questions)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.LessonId == lessonId);

            if (test == null)
                return NotFound(new { error = "No test found for this lesson." });

            return Ok(new
            {
                test.Id,
                test.Title,
                test.MaxPoints,
                test.LessonId,
                Questions = test.Questions.Select(q => new
                {
                    q.Id,
                    q.Text,
                    q.Type,
                    q.Points,
                    Options = string.IsNullOrEmpty(q.OptionsJson)
                        ? new string[] { }
                        : System.Text.Json.JsonSerializer.Deserialize<string[]>(q.OptionsJson)
                })
            });
        }


        // ✅ Get test by test ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var test = await _db.Tests
                .Include(t => t.Questions)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (test == null)
                return NotFound(new { error = "Test not found." });


            return Ok(new
            {
                test.Id,
                test.Title,
                test.MaxPoints,
                test.LessonId,
                questions = test.Questions.Select(q => new
                {
                    q.Id,
                    q.Text,
                    q.Type,
                    q.Points,
                    options = string.IsNullOrEmpty(q.OptionsJson)
                        ? new string[] { }
                        : System.Text.Json.JsonSerializer.Deserialize<string[]>(q.OptionsJson)
                })
            });

        }


        // ✅ Get test by course (if it exists)
        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetByCourse(int courseId)
        {
            var test = await _db.Tests
                .Include(t => t.Questions)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.CourseId == courseId);

            if (test == null)
                return NotFound(new { error = "No test found for this course." });

            return Ok(new
            {
                test.Id,
                test.Title,
                test.MaxPoints,
                test.CourseId,
                Questions = test.Questions.Select(q => new
                {
                    q.Id,
                    q.Text,
                    q.Type,
                    q.Points,
                    Options = string.IsNullOrEmpty(q.OptionsJson)
                        ? new string[] { }
                        : System.Text.Json.JsonSerializer.Deserialize<string[]>(q.OptionsJson)
                })
            });
        }

        // ✅ Submit test answers
        [HttpPost("{testId}/submit")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Submit(int testId, [FromBody] Dictionary<int, string> answers)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var test = await _db.Tests
                .Include(t => t.Questions)
                .FirstOrDefaultAsync(t => t.Id == testId);

            if (test == null)
                return NotFound(new { error = "Test not found." });

            int score = 0;

            foreach (var q in test.Questions)
            {
                if (answers.TryGetValue(q.Id, out var answer) && q.CorrectAnswer == answer)
                    score += q.Points;
            }

            // Save result
            var userTest = new UserTest
            {
                UserId = userId.Value,
                TestId = testId,
                Score = score,
                CompletedAt = DateTime.UtcNow
            };

            _db.UserTests.Add(userTest);

            // Update user total points
            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.TotalPoints += score;
                _db.Users.Update(user);
            }

            await _db.SaveChangesAsync();

            // 🎖 Check for badge unlocks
            await CheckForNewBadgesAsync(userId.Value);

            return Ok(new
            {
                Score = score,
                MaxPoints = test.MaxPoints,
                Percentage = Math.Round((double)score / test.MaxPoints * 100, 1)
            });
        }

        // 🧩 Teacher/Admin: Create a new test
        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Create([FromBody] Test dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { error = "Invalid test data." });

            _db.Tests.Add(dto);
            await _db.SaveChangesAsync();

            return Ok(dto);
        }

        // 🧩 Teacher/Admin: Update test
        [HttpPut("{id}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] Test updated)
        {
            var existing = await _db.Tests.FindAsync(id);
            if (existing == null)
                return NotFound();

            existing.Title = updated.Title ?? existing.Title;
            existing.MaxPoints = updated.MaxPoints;

            _db.Tests.Update(existing);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // ❌ Teacher/Admin: Delete test
        [HttpDelete("{id}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var test = await _db.Tests.FindAsync(id);
            if (test == null) return NotFound();

            _db.Tests.Remove(test);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // 🎖 Helper: Check for badge unlocks
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
                        // TODO: Add logic for completed courses
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
