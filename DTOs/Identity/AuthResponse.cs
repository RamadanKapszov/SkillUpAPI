namespace SkillUpAPI.DTOs.Identity
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }

        public UserDto User { get; set; } = new UserDto();
    }
}
