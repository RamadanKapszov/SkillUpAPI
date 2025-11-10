namespace SkillUpAPI.DTOs.CategoryDtos
{
    public class CategoryWithCoursesDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<CourseSummaryDto> Courses { get; set; } = new();
    }

    public class CourseSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int TeacherId { get; set; }
        public string TeacherUsername { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
