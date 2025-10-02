using System.ComponentModel.DataAnnotations;

namespace SkillUpAPI.DTOs.Identity
{
    public class RegisterRequest
    {
        [Required, StringLength(32, MinimumLength = 3)]
        public string Username { get; set; } = null!;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = null!;

        [Required, StringLength(128, MinimumLength = 6)]
        public string Password { get; set; } = null!;
    }
}
