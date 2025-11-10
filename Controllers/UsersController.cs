using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillUpAPI.Domain.Entities;
using SkillUpAPI.DTOs.UserDTOs;
using SkillUpAPI.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SkillUpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public UsersController(AppDbContext db)
        {
            _db = db;
        }

        // 🧩 Helper – current user ID
        private int? GetUserId()
        {
            var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(sub)) return null;
            return int.TryParse(sub, out var id) ? id : (int?)null;
        }

        // 🧑‍💼 GET: api/users (Admin only)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserInfoDto>>> GetAll()
        {
            var users = await _db.Users
                .AsNoTracking()
                .Select(u => new UserInfoDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role.ToString(),
                    TotalPoints = u.TotalPoints,
                    AvatarUrl = u.AvatarUrl,
                    Bio = u.Bio
                })
                .ToListAsync();

            return Ok(users);
        }

        // 👤 GET: api/users/{id}  (Admin OR the user himself)
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserInfoDto>> GetById(int id)
        {
            var currentUserId = GetUserId();
            var isAdmin = User.IsInRole(nameof(UserRole.Admin));

            if (currentUserId == null)
                return Unauthorized();

            if (!isAdmin && currentUserId != id)
                return Forbid();

            var user = await _db.Users
                .Include(u => u.UserBadges)
                .ThenInclude(ub => ub.Badge)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                Role = user.Role.ToString(),
                user.TotalPoints,
                user.AvatarUrl,
                user.Bio,
                Badges = user.UserBadges.Select(b => new
                {
                    b.Badge.Id,
                    b.Badge.Name,
                    b.Badge.Description,
                    b.Badge.IconUrl,
                    b.AwardedAt
                })
            });
        }

        // ✏️ PUT: api/users/{id}/profile (User updates their own profile)
        [HttpPut("{id:int}/profile")]
        [Authorize(Roles = "Student,Teacher,Admin")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateUserProfileDto dto)
        {
            var currentUserId = GetUserId();
            var isAdmin = User.IsInRole(nameof(UserRole.Admin));

            if (currentUserId == null)
                return Unauthorized();

            if (!isAdmin && currentUserId != id)
                return Forbid();

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.AvatarUrl = dto.AvatarUrl ?? user.AvatarUrl;
            user.Bio = dto.Bio ?? user.Bio;

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // 🛡️ PUT: api/users/{id}/role (Admin only)
        [HttpPut("{id:int}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateUserRoleDto dto)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (!Enum.TryParse<UserRole>(dto.Role, out var newRole))
                return BadRequest(new { error = "Invalid role." });

            user.Role = newRole;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // ❌ DELETE: api/users/{id} (Admin only)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // 🧩 GET: api/users/{id}/dashboard
        [HttpGet("{id:int}/dashboard")]
        [Authorize(Roles = "Student,Teacher,Admin")]
        public async Task<IActionResult> GetDashboard(int id)
        {
            var currentUserId = GetUserId();
            var isAdmin = User.IsInRole(nameof(UserRole.Admin));

            if (currentUserId == null)
                return Unauthorized();

            if (!isAdmin && currentUserId != id)
                return Forbid();

            var user = await _db.Users
                .Include(u => u.UserTests)
                    .ThenInclude(ut => ut.Test)
                .Include(u => u.UserBadges)
                    .ThenInclude(ub => ub.Badge)
                .Include(u => u.Enrollments)
                    .ThenInclude(e => e.Course)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            // ✅ Изчисляваме прогрес (брой завършени уроци / общо уроци)
            var completedLessons = await _db.LessonCompletions
                .CountAsync(lc => lc.UserId == id && lc.CompletedAt != null);

            var totalLessons = await _db.Lessons.CountAsync();
            double progress = totalLessons > 0 ? (double)completedLessons / totalLessons * 100 : 0;

            // ✅ Връщаме обобщена информация
            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.TotalPoints,
                user.AvatarUrl,
                user.Bio,
                ProgressPercent = Math.Round(progress, 1),
                CompletedTests = user.UserTests
                    .OrderByDescending(t => t.CompletedAt)
                    .Take(5)
                    .Select(t => new
                    {
                        t.Id,
                        Title = t.Test.Title,
                        t.Score,
                        t.CompletedAt
                    }),
                Badges = user.UserBadges.Select(b => new
                {
                    b.Badge.Id,
                    b.Badge.Name,
                    b.Badge.Description,
                    b.Badge.IconUrl,
                    b.AwardedAt
                }),
                EnrolledCourses = user.Enrollments.Select(e => new
                {
                    e.Course.Id,
                    e.Course.Title,
                    e.EnrolledAt
                })
            });
        }


    }


}
