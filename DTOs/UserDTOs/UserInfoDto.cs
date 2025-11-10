namespace SkillUpAPI.DTOs.UserDTOs
{
    public class UserInfoDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public int TotalPoints { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
    }
}
