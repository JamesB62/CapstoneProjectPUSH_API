using System.Text.Json.Serialization;

namespace CapstoneProjectPUSH_API.Models
{
    public class PasswordReset
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        // Never sent to the app
        [JsonIgnore]
        public string CodeHash { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public int FailedAttempts { get; set; }
    }
}
