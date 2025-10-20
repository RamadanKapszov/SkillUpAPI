using SkillUpAPI.DTOs.Identity;

namespace SkillUpAPI.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest req);
        Task<AuthResponse> LoginAsync(LoginRequest req);
        Task<UserDto?> GetUserByIdAsync(int userId);

    }
}
