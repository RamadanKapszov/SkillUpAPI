namespace SkillUpAPI.DTOs.CourseDTOs
{
    public class CourseListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int TeacherId { get; set; }
        public string TeacherUsername { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
