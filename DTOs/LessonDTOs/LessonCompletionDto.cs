namespace SkillUpAPI.DTOs.LessonDTOs
{
    public class LessonCompletionDto
    {
        public int LessonId { get; set; }
        public int UserId { get; set; }
        public DateTime CompletedAt { get; set; }
    }
}
