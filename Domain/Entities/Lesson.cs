using System.Collections.Generic;

namespace SkillUpAPI.Domain.Entities
{
    public class Lesson
    {
        public int Id { get; set; }
        public int CourseId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ContentUrl { get; set; }
        public int? Duration { get; set; }
        public string? PreviewImageUrl { get; set; }
        public int OrderIndex { get; set; }

        public Course Course { get; set; } = null!;
        public ICollection<LessonReview> LessonReviews { get; set; } = new List<LessonReview>();
        public ICollection<Test> Tests { get; set; } = new List<Test>();
    }
}
