using CapstoneProjectPUSH_API.Models;

namespace CapstoneProjectPUSH_API.Dtos
{
    // What the app sends from the Update Details form -- the photo is handled separately
    public class UpdateDetailsRequest
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public int? GymId { get; set; }
    }

    // What an admin sends to activate or deactivate an account
    public class UserStatusRequest
    {
        public bool IsActive { get; set; }
    }

    // Reply with the updated user
    public class UserResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public User? User { get; set; }
    }
}