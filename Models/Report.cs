using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapstoneProjectPUSH_API.Models
{
    public class Report
    {
        public int Id { get; set; }

        // Reported workout
        public int WorkoutId { get; set; }

        [NotMapped]
        public string WorkoutTitle { get; set; } = string.Empty;

        [NotMapped]
        public bool WorkoutExists { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        public DateTime DateTimeCreated { get; set; } = DateTime.UtcNow;

        // Reporter info
        public int UserId { get; set; }

        [NotMapped]
        public string UserDisplayName { get; set; } = string.Empty;

        public bool IsResolved { get; set; }

        public DateTime? DateTimeResolved { get; set; }
    }
}
