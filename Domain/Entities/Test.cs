using System.Collections.Generic;

namespace SkillUpAPI.Domain.Entities
{
    public class Test
    {
        public int Id { get; set; }
        public int? CourseId { get; set; }
        public int? LessonId { get; set; }

        public string Title { get; set; } = string.Empty;
        public int MaxPoints { get; set; }

        public Course? Course { get; set; }
        public Lesson? Lesson { get; set; }

        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<UserTest> UserTests { get; set; } = new List<UserTest>();
    }
}
