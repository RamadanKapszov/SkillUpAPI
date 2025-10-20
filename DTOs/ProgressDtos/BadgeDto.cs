namespace SkillUpAPI.DTOs.ProgressDtos
{
    public class BadgeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string ConditionType { get; set; } = null!;
        public int Threshold { get; set; }
        public DateTime? AwardedAt { get; set; }
    }
}
