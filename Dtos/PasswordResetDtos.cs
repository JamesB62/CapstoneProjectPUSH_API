namespace CapstoneProjectPUSH_API.Dtos
{
    // Reset password code request
    public class ResetCodeRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    // Send the code and the new password
    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}