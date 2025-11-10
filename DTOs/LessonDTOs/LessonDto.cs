namespace SkillUpAPI.DTOs.LessonDTOs
{
    public class LessonDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; } = null!;
        public string? ContentUrl { get; set; }
        public int OrderIndex { get; set; }
        public string? Description { get; set; }
        public string? TeacherUsername { get; set; }

        public int? Duration { get; set; }
    }
}
