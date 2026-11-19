using System.Net.Mail;

namespace CapstoneProjectPUSH_API.Services
{
    // Same validation rules as the app's UserService
    public static class AccountRules
    {
        public const int MinDisplayNameLength = 5;
        public const int MaxDisplayNameLength = 60;

        public const string PasswordRequirementsText = "Password must be at least 8 characters and contain at least one letter and one number.";

        public static string NormaliseEmail(string? email)
        {
            if (email == null)
            {
                return string.Empty;
            }

            return email.Trim().ToLowerInvariant();
        }

        public static string NormaliseDisplayName(string? displayName)
        {
            if (displayName == null)
            {
                return string.Empty;
            }

            return displayName.Trim();
        }

        public static bool IsValidDisplayName(string displayName)
        {
            if (displayName.Length < MinDisplayNameLength || displayName.Length > MaxDisplayNameLength)
            {
                return false;
            }

            return true;
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.Length > 254)
            {
                return false;
            }

            try
            {
                MailAddress address = new MailAddress(email);
                return address.Address == email;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static bool IsStrongPassword(string? password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            if (password.Length < 8)
            {
                return false;
            }

            return password.Any(char.IsLetter) && password.Any(char.IsDigit);
        }
    }
}
