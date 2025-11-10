using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillUpAPI.Domain.Entities;
using SkillUpAPI.DTOs.ProgressDtos;
using SkillUpAPI.Persistence;

namespace SkillUpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // only admins can modify badges
    public class BadgesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public BadgesController(AppDbContext db)
        {
            _db = db;
        }

        // ✅ PUBLIC: everyone can see all badges
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<BadgeDto>>> GetAll()
        {
            var badges = await _db.Badges
                .AsNoTracking()
                .Select(b => new BadgeDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    ConditionType = b.ConditionType,
                    Threshold = b.Threshold,
                    IconUrl = b.IconUrl
                })
                .ToListAsync();

            return Ok(badges);
        }

        // ✅ PUBLIC: get badge by id
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<BadgeDto>> GetById(int id)
        {
            var badge = await _db.Badges.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (badge == null) return NotFound();

            return Ok(new BadgeDto
            {
                Id = badge.Id,
                Name = badge.Name,
                Description = badge.Description,
                ConditionType = badge.ConditionType,
                Threshold = badge.Threshold,
                IconUrl = badge.IconUrl
            });
        }

        // ✅ ADMIN: create a new badge
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BadgeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var exists = await _db.Badges.AnyAsync(b => b.Name.ToLower() == dto.Name.ToLower());
            if (exists)
                return Conflict(new { error = "A badge with this name already exists." });

            var badge = new Badge
            {
                Name = dto.Name,
                Description = dto.Description,
                ConditionType = dto.ConditionType,
                Threshold = dto.Threshold,
                IconUrl = dto.IconUrl
            };

            _db.Badges.Add(badge);
            await _db.SaveChangesAsync();

            dto.Id = badge.Id;
            return CreatedAtAction(nameof(GetById), new { id = badge.Id }, dto);
        }

        // ✅ ADMIN: update badge
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] BadgeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var badge = await _db.Badges.FindAsync(id);
            if (badge == null)
                return NotFound();

            badge.Name = dto.Name ?? badge.Name;
            badge.Description = dto.Description ?? badge.Description;
            badge.ConditionType = dto.ConditionType ?? badge.ConditionType;
            badge.Threshold = dto.Threshold ?? badge.Threshold;
            badge.IconUrl = dto.IconUrl ?? badge.IconUrl;

            await _db.SaveChangesAsync();

            return NoContent();
        }

        // ✅ ADMIN: delete badge
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var badge = await _db.Badges.Include(b => b.UserBadges).FirstOrDefaultAsync(b => b.Id == id);
            if (badge == null) return NotFound();

            if (badge.UserBadges.Any())
                return BadRequest(new { error = "Cannot delete badge that is already assigned to users." });

            _db.Badges.Remove(badge);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
