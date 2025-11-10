using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillUpAPI.Persistence;

namespace SkillUpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Only administrators can access
    public class AdminDashboardController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminDashboardController(AppDbContext db)
        {
            _db = db;
        }

        /// ✅ Summary statistics for admin dashboard
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var totalUsers = await _db.Users.CountAsync();
            var totalStudents = await _db.Users.CountAsync(u => u.Role == Domain.Entities.UserRole.Student);
            var totalTeachers = await _db.Users.CountAsync(u => u.Role == Domain.Entities.UserRole.Teacher);
            var totalCourses = await _db.Courses.CountAsync();
            var totalLessons = await _db.Lessons.CountAsync();
            var totalEnrollments = await _db.Enrollments.CountAsync();
            var totalTests = await _db.Tests.CountAsync();
            var totalBadges = await _db.Badges.CountAsync();

            return Ok(new
            {
                totalUsers,
                totalStudents,
                totalTeachers,
                totalCourses,
                totalLessons,
                totalEnrollments,
                totalTests,
                totalBadges
            });
        }

        /// ✅ Top courses by enrollments
        [HttpGet("top-courses")]
        public async Task<IActionResult> GetTopCourses([FromQuery] int top = 5)
        {
            var courses = await _db.Courses
                .Include(c => c.Enrollments)
                .OrderByDescending(c => c.Enrollments.Count)
                .ThenBy(c => c.Title)
                .Take(top)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    Teacher = c.Teacher.Username,
                    EnrolledCount = c.Enrollments.Count
                })
                .ToListAsync();

            return Ok(courses);
        }

        /// ✅ Top students by total points
        [HttpGet("top-students")]
        public async Task<IActionResult> GetTopStudents([FromQuery] int top = 5)
        {
            var students = await _db.Users
                .Where(u => u.Role == Domain.Entities.UserRole.Student)
                .OrderByDescending(u => u.TotalPoints)
                .ThenBy(u => u.Username)
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

        /// ✅ Top teachers by course count
        [HttpGet("top-teachers")]
        public async Task<IActionResult> GetTopTeachers([FromQuery] int top = 5)
        {
            var teachers = await _db.Users
                .Where(u => u.Role == Domain.Entities.UserRole.Teacher)
                .Include(u => u.Courses)
                .OrderByDescending(u => u.Courses.Count)
                .Take(top)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    CourseCount = u.Courses.Count
                })
                .ToListAsync();

            return Ok(teachers);
        }
    }
}
