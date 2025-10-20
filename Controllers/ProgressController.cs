using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillUpAPI.DTOs.ProgressDtos;
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

        public ProgressController(IProgressService progressService)
        {
            _progressService = progressService;
        }

        // helper за извличане на userId от токена
        private int? GetUserId()
        {
            var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(sub)) return null;
            return int.TryParse(sub, out var id) ? id : (int?)null;
        }

        /// ✅ Маркира урок като завършен
        [HttpPost("lessons/{lessonId}/complete")]
        public async Task<IActionResult> CompleteLesson(int lessonId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var success = await _progressService.MarkLessonCompletedAsync(userId.Value, lessonId);
            if (!success) return BadRequest(new { error = "Lesson already completed" });

            return Ok(new { message = "Lesson completed successfully" });
        }

        /// ✅ Връща точките на текущия потребител
        [HttpGet("points")]
        public async Task<IActionResult> GetPoints()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var points = await _progressService.GetUserPointsAsync(userId.Value);
            return Ok(new { points });
        }

        /// ✅ Връща значките на текущия потребител
        [HttpGet("badges")]
        public async Task<IActionResult> GetBadges()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var badges = await _progressService.GetUserBadgesAsync(userId.Value);
            return Ok(badges);
        }

        /// ✅ Връща прогреса на текущия потребител за даден курс
        [HttpGet("courses/{courseId}/progress")]
        public async Task<IActionResult> GetCourseProgress(int courseId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var progress = await _progressService.GetUserCourseProgressAsync(userId.Value, courseId);
            if (progress == null) return NotFound();

            return Ok(progress);
        }
    }
}
