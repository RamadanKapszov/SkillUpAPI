namespace SkillUpAPI.DTOs.LessonDTOs
{
    public class LessonUpdateDto
    {
        public string? Title { get; set; }
        public string? ContentUrl { get; set; }
        public int? OrderIndex { get; set; }
        public string? Description { get; set; }
        public int? Duration { get; set; }
    }
}
