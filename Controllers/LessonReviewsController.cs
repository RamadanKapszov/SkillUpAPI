using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillUpAPI.Domain.Entities;
using SkillUpAPI.Persistence;
using System.Security.Claims;

namespace SkillUpDb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LessonReviewsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public LessonReviewsController(AppDbContext db)
        {
            _db = db;
        }

        // ✅ Get all reviews for a lesson
        [HttpGet("lesson/{lessonId}")]
        public async Task<IActionResult> GetByLesson(int lessonId)
        {
            var reviews = await _db.LessonReviews
                .Where(r => r.LessonId == lessonId)
                .Include(r => r.Student) // optional if you want username
                .Select(r => new
                {
                    r.Id,
                    r.LessonId,
                    r.StudentId,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    StudentUsername = r.Student.Username
                })
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LessonReview model)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdStr == null)
                return Unauthorized();

            int userId = int.Parse(userIdStr);
            model.StudentId = userId;
            model.CreatedAt = DateTime.UtcNow;

            _db.LessonReviews.Add(model);
            await _db.SaveChangesAsync();

            return Ok(model);
        }

    }
}
