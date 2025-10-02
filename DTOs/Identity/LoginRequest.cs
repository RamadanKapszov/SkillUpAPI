using System.ComponentModel.DataAnnotations;

namespace SkillUpAPI.DTOs.Identity
{
    public class LoginRequest
    {
        [Required, StringLength(256)]
        public string UsernameOrEmail { get; set; } = null!;

        [Required, StringLength(128, MinimumLength = 6)]
        public string Password { get; set; } = null!;
    }
}
