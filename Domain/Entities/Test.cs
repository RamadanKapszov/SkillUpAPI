using System.Collections.Generic;

namespace SkillUpAPI.Domain.Entities
{
    public class Test
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;
        public string Title { get; set; } = null!;
        public int MaxPoints { get; set; }
        public List<Question> Questions { get; set; } = new();
        public List<Attempt> Attempts { get; set; } = new();
    }
}
