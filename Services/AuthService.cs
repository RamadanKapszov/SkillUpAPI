using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SkillUpAPI.Domain.Entities;
using SkillUpAPI.DTOs.Identity;
using SkillUpAPI.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SkillUpAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _cfg;

        public AuthService(AppDbContext db, IConfiguration cfg)
        {
            _db = db;
            _cfg = cfg;
        }

        // ✅ Register new user
        public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
        {
            if (await _db.Users.AnyAsync(u => u.Username == req.Username))
                throw new InvalidOperationException("Username already taken.");
            if (await _db.Users.AnyAsync(u => u.Email == req.Email))
                throw new InvalidOperationException("Email already registered.");

            var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);

            var user = new User
            {
                Username = req.Username,
                Email = req.Email,
                PasswordHash = hash,
                Role = UserRole.Student,
                TotalPoints = 0,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return GenerateAuthResponse(user);
        }

        // ✅ Login user
        public async Task<AuthResponse> LoginAsync(LoginRequest req)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Username == req.UsernameOrEmail || u.Email == req.UsernameOrEmail);

            if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                throw new InvalidOperationException("Invalid credentials.");

            return GenerateAuthResponse(user);
        }

        // ✅ Generate JWT and user data
        private AuthResponse GenerateAuthResponse(User user)
        {
            var jwtSection = _cfg.GetSection("Jwt");
            var secret = jwtSection.GetValue<string>("Secret")!;
            var issuer = jwtSection.GetValue<string>("Issuer");
            var audience = jwtSection.GetValue<string>("Audience");
            var expiresMinutes = jwtSection.GetValue<int>("ExpiresMinutes", 60); // default 1h

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAt,
                signingCredentials: creds);

            var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

            return new AuthResponse
            {
                Token = tokenStr,
                ExpiresAt = expiresAt,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    TotalPoints = user.TotalPoints
                }
            };
        }

        // ✅ Get user by ID
        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user == null ? null : new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString(),
                TotalPoints = user.TotalPoints
            };
        }
    }
}
