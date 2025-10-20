using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillUpAPI.DTOs;
using SkillUpAPI.DTOs.EnrollmentDTOs;
using SkillUpAPI.Persistence;
using SkillUpAPI.Services;
using System.Security.Claims;

namespace SkillUpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EnrollmentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IProgressService _progressService;

        public EnrollmentsController(AppDbContext context, IProgressService progressService)
        {
            _context = context;
            _progressService = progressService;
        }

        // ---------------------------
        // Student: enroll in a course
        // ---------------------------
        [HttpPost]
        public async Task<IActionResult> Enroll(EnrollRequest dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Check if already enrolled
            var existing = await _context.Enrollments
                .AnyAsync(e => e.UserId == userId && e.CourseId == dto.CourseId);
            if (existing) return BadRequest("Already enrolled.");

            var enrollment = new Domain.Entities.Enrollment
            {
                UserId = userId,
                CourseId = dto.CourseId,
                EnrolledAt = DateTime.UtcNow
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            // Add points and check badges
            await _progressService.AddPointsAsync(userId, 10);

            return Ok(new { message = "Enrolled successfully" });
        }

        // ---------------------------
        // Student: view my enrollments
        // ---------------------------
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<EnrollmentDto>>> GetMyEnrollments()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .Include(e => e.User)
                .Where(e => e.UserId == userId)
                .Select(e => new EnrollmentDto
                {
                    Id = e.Id,
                    CourseId = e.CourseId,
                    CourseTitle = e.Course.Title,
                    UserId = e.UserId,
                    Username = e.User.Username,
                    EnrolledAt = e.EnrolledAt
                })
                .ToListAsync();

            return Ok(enrollments);
        }

        // ---------------------------
        // Teacher/Admin: view students for a course
        // ---------------------------
        [HttpGet("course/{courseId}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<ActionResult<IEnumerable<EnrollmentDto>>> GetEnrollmentsForCourse(int courseId)
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .Include(e => e.User)
                .Where(e => e.CourseId == courseId)
                .Select(e => new EnrollmentDto
                {
                    Id = e.Id,
                    CourseId = e.CourseId,
                    CourseTitle = e.Course.Title,
                    UserId = e.UserId,
                    Username = e.User.Username,
                    EnrolledAt = e.EnrolledAt
                })
                .ToListAsync();

            return Ok(enrollments);
        }

        // ---------------------------
        // Teacher/Admin: remove a student from a course
        // ---------------------------
        [HttpDelete("{enrollmentId}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> RemoveEnrollment(int enrollmentId)
        {
            var enrollment = await _context.Enrollments.FindAsync(enrollmentId);
            if (enrollment == null) return NotFound();

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Enrollment removed successfully" });
        }
    }
}
