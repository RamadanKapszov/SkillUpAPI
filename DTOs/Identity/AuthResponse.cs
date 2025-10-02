namespace SkillUpAPI.DTOs.Identity
{
    public class AuthResponse
    {
        public string Token { get; set; } = null!;
        public int ExpiresInSeconds { get; set; }
        public UserDto User { get; set; } = null!;
    }
}
