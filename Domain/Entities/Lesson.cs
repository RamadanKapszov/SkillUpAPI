namespace SkillUpAPI.Domain.Entities
{
    public class Lesson
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? ContentUrl { get; set; }
        public int OrderIndex { get; set; } = 0;
    }
}
