namespace SkillUpAPI.DTOs.EnrollmentDTOs
{
    public class EnrollmentDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = null!;
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string TeacherName { get; set; } = null!;
        public DateTime EnrolledAt { get; set; }
    }
}
