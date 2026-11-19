namespace CapstoneProjectPUSH_API.Dtos
{
    // Step 1: ask for a reset code
    public class ResetCodeRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    // Step 2: send the code and the new password
    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
