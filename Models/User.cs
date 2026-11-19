using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CapstoneProjectPUSH_API.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(60)]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string Email { get; set; } = string.Empty;

        // Never sent to the app
        [JsonIgnore]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime DateTimeCreated { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string ProfileImageUrl { get; set; } = string.Empty;

        public bool IsAdmin { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public bool IsGuest { get; set; } = false;

        public int? GymId { get; set; }
    }
}
