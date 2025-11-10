using System;
using System.Collections.Generic;

namespace SkillUpAPI.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public UserRole Role { get; set; } = UserRole.Student;
        public int TotalPoints { get; set; } = 0;
        public DateTime CreatedAt { get; set; }

        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }

        // Navigation properties
        public List<Course> Courses { get; set; } = new();
        public List<UserBadge> UserBadges { get; set; } = new();
        public List<Enrollment> Enrollments { get; set; } = new();
        public List<UserTest> UserTests { get; set; } = new();
        public List<LessonReview> LessonReviews { get; set; } = new();
        public List<LessonCompletion> LessonCompletions { get; set; } = new();
    }
}
