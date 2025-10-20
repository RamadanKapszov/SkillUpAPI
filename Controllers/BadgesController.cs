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
    [Authorize(Roles = "Admin")] // само админ може да управлява значки
    public class BadgesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public BadgesController(AppDbContext db)
        {
            _db = db;
        }

        // GET /api/badges
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BadgeDto>>> GetAll()
        {
            var badges = await _db.Badges
                .AsNoTracking()
                .Select(b => new BadgeDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description
                })
                .ToListAsync();

            return Ok(badges);
        }

        // GET /api/badges/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<BadgeDto>> GetById(int id)
        {
            var badge = await _db.Badges.FindAsync(id);
            if (badge == null) return NotFound();

            return Ok(new BadgeDto
            {
                Id = badge.Id,
                Name = badge.Name,
                Description = badge.Description
            });
        }

        // POST /api/badges
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BadgeDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var badge = new Badge
            {
                Name = dto.Name,
                Description = dto.Description
            };

            _db.Badges.Add(badge);
            await _db.SaveChangesAsync();

            dto.Id = badge.Id;
            return CreatedAtAction(nameof(GetById), new { id = badge.Id }, dto);
        }

        // PUT /api/badges/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] BadgeDto dto)
        {
            var badge = await _db.Badges.FindAsync(id);
            if (badge == null) return NotFound();

            badge.Name = dto.Name;
            badge.Description = dto.Description;

            _db.Badges.Update(badge);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // DELETE /api/badges/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var badge = await _db.Badges.FindAsync(id);
            if (badge == null) return NotFound();

            _db.Badges.Remove(badge);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
