using System;

namespace SkillUpAPI.Domain.Entities
{
    public class UserBadge
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int BadgeId { get; set; }
        public Badge Badge { get; set; } = null!;
        public DateTime AwardedAt { get; set; }
    }
}
