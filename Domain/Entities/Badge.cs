using System.Collections.Generic;

namespace SkillUpAPI.Domain.Entities
{
    public class Badge
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public BadgeCondition ConditionType { get; set; } = BadgeCondition.TotalPoints;
        public int Threshold { get; set; }
        public List<UserBadge> UserBadges { get; set; } = new();
    }
}
