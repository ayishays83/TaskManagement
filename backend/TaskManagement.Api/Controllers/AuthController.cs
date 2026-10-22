
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using TaskManagement.Api.Data;
using TaskManagement.Api.Models;
using TaskManagement.Api.Services;

namespace TaskManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;

        public AuthController(
            AppDbContext db,
            IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public class RegisterRequest
        {
            public string FullName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
            public string Role { get; set; } = "User";
        }

        public class LoginRequest
        {
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "All fields are required." });
            }

            if (request.Password.Length < 8)
            {
                return BadRequest(new { message = "Password must be at least 8 characters." });
            }

            var email = request.Email.Trim().ToLowerInvariant();

            if (await _db.Users.AnyAsync(u => u.Email == email))
            {
                return Conflict(new { message = "Email already registered." });
            }

            // Public registration must never grant privileged roles.
            var user = new AppUser
            {
                FullName = request.FullName.Trim(),
                Email = email,
                PasswordHash = PasswordHelper.Hash(request.Password),
                Role = "User"
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Registration successful.",
                user.Id,
                user.FullName,
                user.Email,
                user.Role
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var user = await _db.Users.FirstOrDefaultAsync(
                u => u.Email == email && u.IsActive);

            if (user == null ||
!PasswordHelper.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            var jwt = _configuration.GetSection("Jwt");
            var keyText = jwt["Key"];

            if (string.IsNullOrWhiteSpace(keyText) ||
                Encoding.UTF8.GetByteCount(keyText) < 32)
            {
                return StatusCode(500, new
                {
                    message = "JWT configuration is invalid."
                });
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(keyText));

            var credentials = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    double.TryParse(jwt["ExpiryMinutes"], out var minutes)
                        ? minutes : 60),
                signingCredentials: credentials);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiresAt = token.ValidTo,
                user = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.Role
                }
            });
        }

        private static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, 100000,
                HashAlgorithmName.SHA256, 32);

            return Convert.ToBase64String(salt) + "." +
                   Convert.ToBase64String(hash);
        }

        private static bool VerifyPassword(
            string password, string storedHash)
        {
            try
            {
                var parts = storedHash.Split('.');
                if (parts.Length != 2) return false;

                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] expected = Convert.FromBase64String(parts[1]);
                byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                    password, salt, 100000,
                    HashAlgorithmName.SHA256, expected.Length);

                return CryptographicOperations.FixedTimeEquals(
                    actual, expected);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
