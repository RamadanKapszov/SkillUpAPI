using SkillUpAPI.DTOs.LessonDTOs;
using SkillUpAPI.DTOs.ProgressDtos;

namespace SkillUpAPI.Services
{
    public interface IProgressService
    {
        Task AddPointsAsync(int userId, int points);
        Task<int> GetUserPointsAsync(int userId);
        Task<List<BadgeDto>> GetUserBadgesAsync(int userId);
        Task AddBadgeAsync(int userId, int badgeId);
        Task<bool> MarkLessonCompletedAsync(int userId, int lessonId);
        Task<IEnumerable<LessonCompletionDto>> GetCompletedLessonsAsync(int userId, int courseId);
        Task<UserCourseProgressDto> GetUserCourseProgressAsync(int userId, int courseId);
    }
}
