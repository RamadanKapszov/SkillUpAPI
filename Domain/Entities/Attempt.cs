using System;

namespace SkillUpAPI.Domain.Entities
{
    public class Attempt
    {
        public int Id { get; set; }
        public int TestId { get; set; }
        public Test Test { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int Score { get; set; }
        public DateTime AttemptedAt { get; set; }
    }
}
