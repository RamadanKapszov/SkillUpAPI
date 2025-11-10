using System;

namespace SkillUpAPI.Domain.Entities
{
    public class UserTest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TestId { get; set; }
        public int Score { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public Test Test { get; set; } = null!;
    }
}
