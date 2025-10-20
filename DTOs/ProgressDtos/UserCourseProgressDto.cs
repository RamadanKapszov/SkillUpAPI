namespace SkillUpAPI.DTOs.ProgressDtos
{
    public class UserCourseProgressDto
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = null!;
        public int CompletedLessons { get; set; }
        public int TotalLessons { get; set; }
        public int Points { get; set; }
        public List<string> Badges { get; set; } = new();
    }

}
