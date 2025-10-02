using System;
using System.Collections.Generic;

namespace SkillUpAPI.Domain.Entities
{
    public class Course
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
        public int TeacherId { get; set; }
        public User Teacher { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public List<Lesson> Lessons { get; set; } = new();
        public List<Test> Tests { get; set; } = new();
        public List<Enrollment> Enrollments { get; set; } = new();
    }
}
