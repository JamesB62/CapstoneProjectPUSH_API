using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using CapstoneProjectPUSH_API.Models;

namespace CapstoneProjectPUSH_API.Services
{
    // Creates the login token the app sends with each request
    public class TokenService
    {
        public const string AdminRole = "Admin";
        public const string MemberRole = "Member";
        public const string GuestRole = "Guest";

        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreateToken(User user)
        {
            string role;

            if (user.IsAdmin)
            {
                role = AdminRole;
            }
            else if (user.IsGuest)
            {
                role = GuestRole;
            }
            else
            {
                role = MemberRole;
            }

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.DisplayName),
                new Claim(ClaimTypes.Role, role)
            };

            string keyText = GetKey(_configuration);
            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyText));
            SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = credentials
            };

            JsonWebTokenHandler handler = new JsonWebTokenHandler();
            return handler.CreateToken(descriptor);
        }

        // Shared with Program.cs so both use the same key
        public static string GetKey(IConfiguration configuration)
        {
            string? keyText = configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(keyText) || keyText.Length < 32)
            {
                throw new InvalidOperationException("Jwt:Key is missing or shorter than 32 characters.");
            }

            return keyText;
        }
    }
}
