using SkillUpAPI.Domain.Entities;

public class Badge
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ConditionType { get; set; }
    public int? Threshold { get; set; }
    public string? IconUrl { get; set; }

    public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}
