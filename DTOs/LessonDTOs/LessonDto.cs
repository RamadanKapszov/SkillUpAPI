namespace SkillUpAPI.DTOs.LessonDTOs
{
    public class LessonDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; } = null!;
        public string? ContentUrl { get; set; }
        public int OrderIndex { get; set; }
    }
}
