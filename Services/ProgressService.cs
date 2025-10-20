using Microsoft.EntityFrameworkCore;
using SkillUpAPI.Domain.Entities;
using SkillUpAPI.DTOs.LessonDTOs;
using SkillUpAPI.DTOs.ProgressDtos;
using SkillUpAPI.Persistence;

namespace SkillUpAPI.Services
{
    public class ProgressService : IProgressService
    {
        private readonly AppDbContext _db;

        public ProgressService(AppDbContext db)
        {
            _db = db;
        }

        // Добавяне на точки на потребител
        public async Task AddPointsAsync(int userId, int points)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return;

            user.TotalPoints += points;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
        }

        // Връща текущите точки на потребителя
        public async Task<int> GetUserPointsAsync(int userId)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            return user?.TotalPoints ?? 0;
        }

        // Връща всички значки на потребителя
        public async Task<List<BadgeDto>> GetUserBadgesAsync(int userId)
        {
            return await _db.UserBadges
                .AsNoTracking()
                .Include(ub => ub.Badge)
                .Where(ub => ub.UserId == userId)
                .Select(ub => new BadgeDto
                {
                    Id = ub.BadgeId,
                    Name = ub.Badge.Name,
                    Description = ub.Badge.Description,
                    ConditionType = ub.Badge.ConditionType.ToString(),
                    Threshold = ub.Badge.Threshold,
                    AwardedAt = ub.AwardedAt
                })
                .ToListAsync();
        }

        // Добавя значка на потребител (може да се ползва вътрешно)
        public async Task AddBadgeAsync(int userId, int badgeId)
        {
            var exists = await _db.UserBadges.AnyAsync(ub => ub.UserId == userId && ub.BadgeId == badgeId);
            if (exists) return;

            _db.UserBadges.Add(new UserBadge
            {
                UserId = userId,
                BadgeId = badgeId,
                AwardedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        // Маркира урок като завършен и начислява точки / проверява значки
        public async Task<bool> MarkLessonCompletedAsync(int userId, int lessonId)
        {
            var exists = await _db.LessonCompletions
                .AnyAsync(lc => lc.UserId == userId && lc.LessonId == lessonId);
            if (exists) return false; // вече е завършен

            _db.LessonCompletions.Add(new LessonCompletion
            {
                UserId = userId,
                LessonId = lessonId,
                CompletedAt = DateTime.UtcNow
            });

            // Добавяне на точки за завършен урок
            await AddPointsAsync(userId, 5);

            await _db.SaveChangesAsync();

            // Проверка и добавяне на значки
            await CheckAndAwardBadgesAsync(userId);

            return true;
        }

        // Връща всички завършени уроки за даден курс
        public async Task<IEnumerable<LessonCompletionDto>> GetCompletedLessonsAsync(int userId, int courseId)
        {
            return await _db.LessonCompletions
                .AsNoTracking()
                .Include(lc => lc.Lesson)
                .Where(lc => lc.UserId == userId && lc.Lesson.CourseId == courseId)
                .Select(lc => new LessonCompletionDto
                {
                    LessonId = lc.LessonId,
                    UserId = lc.UserId,
                    CompletedAt = lc.CompletedAt
                })
                .ToListAsync();
        }

        // Връща прогреса на потребителя в даден курс (точки, завършени уроки, значки)
        public async Task<UserCourseProgressDto> GetUserCourseProgressAsync(int userId, int courseId)
        {
            var course = await _db.Courses
                .Include(c => c.Lessons)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null) return null!;

            var completedCount = await _db.LessonCompletions
                .CountAsync(lc => lc.UserId == userId && lc.Lesson.CourseId == courseId);

            var user = await _db.Users.FindAsync(userId);

            var badges = await _db.UserBadges
                .Include(ub => ub.Badge)
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.Badge.Name)
                .ToListAsync();

            return new UserCourseProgressDto
            {
                CourseId = courseId,
                CourseTitle = course.Title,
                CompletedLessons = completedCount,
                TotalLessons = course.Lessons.Count,
                Points = user?.TotalPoints ?? 0,
                Badges = badges
            };
        }

        // Вътрешен метод: проверка и присъждане на значки
        private async Task CheckAndAwardBadgesAsync(int userId)
        {
            var user = await _db.Users
                .Include(u => u.UserBadges)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return;

            var allBadges = await _db.Badges.ToListAsync();

            foreach (var badge in allBadges)
            {
                bool alreadyAwarded = user.UserBadges.Any(ub => ub.BadgeId == badge.Id);
                if (alreadyAwarded) continue;

                bool conditionMet = badge.ConditionType.ToString() switch
                {
                    "Points" => user.TotalPoints >= badge.Threshold,
                    "LessonsCompleted" => await _db.LessonCompletions
                        .CountAsync(lc => lc.UserId == userId) >= badge.Threshold,
                    _ => false
                };

                if (conditionMet)
                {
                    _db.UserBadges.Add(new UserBadge
                    {
                        UserId = userId,
                        BadgeId = badge.Id,
                        AwardedAt = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}
