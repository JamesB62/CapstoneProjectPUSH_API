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

        // Finds a user's role
        public static string GetRole(User user)
        {
            if (user.IsAdmin)
            {
                return AdminRole;
            }
            else if (user.IsGuest)
            {
                return GuestRole;
            }
            else
            {
                return MemberRole;
            }
        }

        // Creates a user token
        public string CreateToken(User user)
        {
            // Finds the current user's role
            string role = GetRole(user);

            // Important info about the current user stored in the token
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.DisplayName),
                new Claim(ClaimTypes.Role, role)
            };

            // Secures the token key -- stored in appsettings or user secrets
            string keyText = GetKey(_configuration);
            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyText));
            SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Creates the token, with an experation date
            SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = credentials
            };

            JsonWebTokenHandler handler = new JsonWebTokenHandler();
            return handler.CreateToken(descriptor);
        }

        // Aborts token methods early if token is missing or shorter than expecteed -- shared with Program.cs so both use the same key
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