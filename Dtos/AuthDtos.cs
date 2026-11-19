using CapstoneProjectPUSH_API.Models;

namespace CapstoneProjectPUSH_API.Dtos
{
    // What the app sends to register
    public class RegisterRequest
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public bool HasAcceptedPrivacy { get; set; }
    }

    // What the app sends to log in
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // What the API sends back (same idea as the app's AuthResult, plus the token)
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public User? User { get; set; }
    }
}
