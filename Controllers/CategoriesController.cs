using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillUpAPI.Domain.Entities;
using SkillUpAPI.DTOs.CategoryDtos;
using SkillUpAPI.Persistence;

namespace SkillUpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CategoriesController(AppDbContext db)
        {
            _db = db;
        }

        // ✅ PUBLIC: Get all categories (everyone can see)
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
        {
            var categories = await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    CourseCount = c.Courses.Count,
                    Description = c.Description
                })
                .ToListAsync();

            return Ok(categories);
        }

        // ✅ PUBLIC: Get a single category + its courses
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<CategoryWithCoursesDto>> GetById(int id)
        {
            var category = await _db.Categories
                .Include(c => c.Courses)
                    .ThenInclude(cr => cr.Teacher)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            var dto = new CategoryWithCoursesDto
            {
                Id = category.Id,
                Name = category.Name,
                Courses = category.Courses.Select(cr => new CourseSummaryDto
                {
                    Id = cr.Id,
                    Title = cr.Title,
                    Description = cr.Description,
                    TeacherId = cr.TeacherId,
                    TeacherUsername = cr.Teacher.Username,
                    CreatedAt = cr.CreatedAt
                   
                   
                }).ToList()
            };

            return Ok(dto);
        }

        // ✅ ADMIN: Create category
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var exists = await _db.Categories.AnyAsync(c => c.Name.ToLower() == dto.Name.ToLower());
            if (exists)
                return Conflict(new { error = "Category already exists." });

            var category = new Category { Name = dto.Name.Trim() };
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            dto.Id = category.Id;
            return CreatedAtAction(nameof(GetById), new { id = category.Id }, dto);
        }

        // ✅ ADMIN: Update category name
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var category = await _db.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            var duplicate = await _db.Categories.AnyAsync(c => c.Id != id && c.Name.ToLower() == dto.Name.ToLower());
            if (duplicate)
                return Conflict(new { error = "Category with this name already exists." });

            category.Name = dto.Name.Trim();
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // ✅ ADMIN: Delete category (only if no courses are linked)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.Categories
                .Include(c => c.Courses)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            if (category.Courses.Any())
                return BadRequest(new { error = "Cannot delete category with existing courses." });

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
