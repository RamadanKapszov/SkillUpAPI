using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillUpAPI.Persistence;

namespace SkillUpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // само администратор
    public class AdminDashboardController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminDashboardController(AppDbContext db)
        {
            _db = db;
        }

        // GET /api/admin-dashboard/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var totalUsers = await _db.Users.CountAsync();
            var totalStudents = await _db.Users.CountAsync(u => u.Role == Domain.Entities.UserRole.Student);
            var totalTeachers = await _db.Users.CountAsync(u => u.Role == Domain.Entities.UserRole.Teacher);
            var totalCourses = await _db.Courses.CountAsync();
            var totalLessons = await _db.Lessons.CountAsync();
            var totalEnrollments = await _db.Enrollments.CountAsync();

            return Ok(new
            {
                totalUsers,
                totalStudents,
                totalTeachers,
                totalCourses,
                totalLessons,
                totalEnrollments
            });
        }

        // GET /api/admin-dashboard/top-courses
        [HttpGet("top-courses")]
        public async Task<IActionResult> GetTopCourses([FromQuery] int top = 5)
        {
            var courses = await _db.Courses
                .Include(c => c.Enrollments)
                .OrderByDescending(c => c.Enrollments.Count)
                .Take(top)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    EnrolledCount = c.Enrollments.Count
                })
                .ToListAsync();

            return Ok(courses);
        }

        // GET /api/admin-dashboard/top-students
        [HttpGet("top-students")]
        public async Task<IActionResult> GetTopStudents([FromQuery] int top = 5)
        {
            var students = await _db.Users
                .Where(u => u.Role == Domain.Entities.UserRole.Student)
                .OrderByDescending(u => u.TotalPoints)
                .Take(top)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.TotalPoints
                })
                .ToListAsync();

            return Ok(students);
        }
    }
}
