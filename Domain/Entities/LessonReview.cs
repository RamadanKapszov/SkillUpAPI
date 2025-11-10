using System;

namespace SkillUpAPI.Domain.Entities
{
    public class LessonReview
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public int StudentId { get; set; }
        public int Rating { get; set; }  // 1–5
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Lesson? Lesson { get; set; }
        public User? Student { get; set; }
    }
}