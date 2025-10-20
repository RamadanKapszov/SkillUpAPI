using Microsoft.EntityFrameworkCore;
using SkillUpAPI.Domain.Entities;

namespace SkillUpAPI.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Course> Courses { get; set; } = null!;
        public DbSet<Lesson> Lessons { get; set; } = null!;
        public DbSet<Test> Tests { get; set; } = null!;
        public DbSet<Question> Questions { get; set; } = null!;
        public DbSet<Attempt> Attempts { get; set; } = null!;
        public DbSet<Badge> Badges { get; set; } = null!;
        public DbSet<UserBadge> UserBadges { get; set; } = null!;
        public DbSet<Enrollment> Enrollments { get; set; } = null!;
        public DbSet<LessonCompletion> LessonCompletions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder b)
        {
            // Users
            b.Entity<User>(e =>
            {
                e.ToTable("Users");
                e.HasIndex(x => x.Username).IsUnique();
                e.HasIndex(x => x.Email).IsUnique();
                e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.TotalPoints).HasDefaultValue(0);
            });

            // Categories
            b.Entity<Category>(e =>
            {
                e.ToTable("Categories");
                e.HasIndex(x => x.Name).IsUnique();
            });

            // Courses
            b.Entity<Course>(e =>
            {
                e.ToTable("Courses");
                e.HasOne(x => x.Category).WithMany(x => x.Courses).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
                e.HasOne(x => x.Teacher)
 .WithMany(x => x.Courses)
 .HasForeignKey(x => x.TeacherId)
 .OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => x.CategoryId);
                e.HasIndex(x => x.TeacherId);
            });

            // Lessons
            b.Entity<Lesson>(e =>
            {
                e.ToTable("Lessons");
                e.HasOne(x => x.Course).WithMany(x => x.Lessons).HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
                e.Property(x => x.OrderIndex).HasDefaultValue(0);
                e.HasIndex(x => x.CourseId);
            });

            // Tests
            b.Entity<Test>(e =>
            {
                e.ToTable("Tests");
                e.HasOne(x => x.Course).WithMany(x => x.Tests).HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => x.CourseId);
            });

            // Questions
            b.Entity<Question>(e =>
            {
                e.ToTable("Questions");
                e.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
                e.HasOne(x => x.Test).WithMany(x => x.Questions).HasForeignKey(x => x.TestId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => x.TestId);
            });

            // Attempts
            b.Entity<Attempt>(e =>
            {
                e.ToTable("Attempts");
                e.HasOne(x => x.Test).WithMany(x => x.Attempts).HasForeignKey(x => x.TestId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.User).WithMany(x => x.Attempts).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.TestId);
            });

            // Badges
            b.Entity<Badge>(e =>
            {
                e.ToTable("Badges");
                e.Property(x => x.ConditionType).HasConversion<string>().HasMaxLength(30);
                e.HasIndex(x => x.Name).IsUnique();
            });

            // UserBadges
            b.Entity<UserBadge>(e =>
            {
                e.ToTable("UserBadges");
                e.HasOne(x => x.User).WithMany(x => x.UserBadges).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Badge).WithMany(x => x.UserBadges).HasForeignKey(x => x.BadgeId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.UserId, x.BadgeId }).IsUnique();
                e.HasIndex(x => x.UserId);
            });

            // Enrollments
            b.Entity<Enrollment>(e =>
            {
                e.ToTable("Enrollments");
                e.HasOne(x => x.User).WithMany(x => x.Enrollments).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.Course).WithMany(x => x.Enrollments).HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.UserId, x.CourseId }).IsUnique();
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.CourseId);
            });

            // --- Seed minimal data (IDs must be constants) ---
            var now = new DateTime(2025, 8, 22, 0, 0, 0, DateTimeKind.Utc);

            b.Entity<User>().HasData(
                new User { Id = 1, Username = "admin", Email = "admin@example.com", PasswordHash = "REPLACE_WITH_HASH", Role = UserRole.Admin, TotalPoints = 0, CreatedAt = now },
                new User { Id = 2, Username = "teacher", Email = "teacher@example.com", PasswordHash = "REPLACE_WITH_HASH", Role = UserRole.Teacher, TotalPoints = 0, CreatedAt = now },
                new User { Id = 3, Username = "student", Email = "student@example.com", PasswordHash = "REPLACE_WITH_HASH", Role = UserRole.Student, TotalPoints = 0, CreatedAt = now }
            );

            b.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Programming" },
                new Category { Id = 2, Name = "Mathematics" }
            );

            b.Entity<Course>().HasData(
                new Course { Id = 1, Title = "C# Basics", Description = "Intro to C#", CategoryId = 1, TeacherId = 2, CreatedAt = now }
            );

            b.Entity<Lesson>().HasData(
                new Lesson { Id = 1, CourseId = 1, Title = "Intro", ContentUrl = "https://example.com/intro", OrderIndex = 0 },
                new Lesson { Id = 2, CourseId = 1, Title = "Variables", ContentUrl = "https://example.com/variables", OrderIndex = 1 }
            );

            b.Entity<Test>().HasData(
                new Test { Id = 1, CourseId = 1, Title = "Quiz 1", MaxPoints = 100 }
            );

            b.Entity<Question>().HasData(
                new Question { Id = 1, TestId = 1, Text = "What is C#?", Type = QuestionType.Single, OptionsJson = "[\"Language\",\"Database\",\"OS\"]", CorrectAnswer = "Language" },
                new Question { Id = 2, TestId = 1, Text = "Select value types", Type = QuestionType.Multiple, OptionsJson = "[\"int\",\"string\",\"bool\"]", CorrectAnswer = "[\"int\",\"bool\"]" }
            );

            b.Entity<Badge>().HasData(
                new Badge { Id = 1, Name = "Rookie", Description = "Earn 100 points", ConditionType = BadgeCondition.TotalPoints, Threshold = 100 },
                new Badge { Id = 2, Name = "Finisher", Description = "Complete a course", ConditionType = BadgeCondition.CourseCompleted, Threshold = 1 }
            );
        }
    }
}
