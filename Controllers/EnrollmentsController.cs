using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillUpAPI.DTOs;
using SkillUpAPI.DTOs.EnrollmentDTOs;
using SkillUpAPI.Domain.Entities;
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

        // Helper for current user id
        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // ✅ Student: Enroll in a course
        [HttpPost]
        public async Task<IActionResult> Enroll([FromBody] EnrollRequest dto)
        {
            if (dto == null || dto.CourseId <= 0)
                return BadRequest(new { error = "Invalid course ID." });

            var userId = GetUserId();

            var course = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == dto.CourseId);

            if (course == null)
                return NotFound(new { error = "Course not found." });

            // Prevent teachers from enrolling in their own course
            if (course.TeacherId == userId)
                return BadRequest(new { error = "Teachers cannot enroll in their own course." });

            // Prevent duplicate enrollments
            bool alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.UserId == userId && e.CourseId == dto.CourseId);

            if (alreadyEnrolled)
                return BadRequest(new { error = "Already enrolled in this course." });

            var enrollment = new Enrollment
            {
                UserId = userId,
                CourseId = dto.CourseId,
                EnrolledAt = DateTime.UtcNow
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            // 🎯 Add +10 points for enrolling
            await _progressService.AddPointsAsync(userId, 10);
            await _progressService.CheckForNewBadgesAsync(userId);

            return Ok(new
            {
                message = "Enrollment successful.",
                enrollment.Id,
                courseTitle = course.Title,
                teacher = course.Teacher.Username,
                enrolledAt = enrollment.EnrolledAt
            });
        }

        [HttpDelete("{courseId}")]
        public async Task<IActionResult> Unenroll(int courseId)
        {
            var userId = GetUserId();

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == courseId && e.UserId == userId);

            if (enrollment == null)
                return BadRequest(new { error = "Not enrolled in this course." });

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Successfully unenrolled from course." });
        }



        // ✅ Student: View my enrollments
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<EnrollmentDto>>> GetMyEnrollments()
        {
            var userId = GetUserId();

            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                    .ThenInclude(c => c.Teacher)
                .Where(e => e.UserId == userId)
                .Select(e => new EnrollmentDto
                {
                    Id = e.Id,
                    CourseId = e.CourseId,
                    CourseTitle = e.Course.Title,
                    TeacherName = e.Course.Teacher.Username,
                    UserId = e.UserId,
                    Username = e.User.Username,
                    EnrolledAt = e.EnrolledAt
                })
                .ToListAsync();

            return Ok(enrollments);
        }

        // ✅ Teacher/Admin: View students for a specific course
        [HttpGet("course/{courseId}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<ActionResult<IEnumerable<EnrollmentDto>>> GetEnrollmentsForCourse(int courseId)
        {
            var userId = GetUserId();
            var isAdmin = User.IsInRole(nameof(UserRole.Admin));

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null) return NotFound();

            // Allow only the teacher or an admin
            if (!isAdmin && course.TeacherId != userId)
                return Forbid();

            var enrollments = await _context.Enrollments
                .Include(e => e.User)
                .Include(e => e.Course)
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

        //// ✅ Teacher/Admin: Remove a student from a course
        //[HttpDelete("{enrollmentId}")]
        //[Authorize(Roles = "Teacher,Admin")]
        //public async Task<IActionResult> RemoveEnrollment(int enrollmentId)
        //{
        //    var enrollment = await _context.Enrollments
        //        .Include(e => e.Course)
        //        .FirstOrDefaultAsync(e => e.Id == enrollmentId);

        //    if (enrollment == null)
        //        return NotFound();

        //    var userId = GetUserId();
        //    var isAdmin = User.IsInRole(nameof(UserRole.Admin));

        //    // only admin or the teacher of the course can remove
        //    if (!isAdmin && enrollment.Course.TeacherId != userId)
        //        return Forbid();

        //    _context.Enrollments.Remove(enrollment);
        //    await _context.SaveChangesAsync();

        //    return Ok(new { message = "Enrollment removed successfully." });
        //}
    }
}
