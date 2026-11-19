using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapstoneProjectPUSH_API.Models
{
    public class Note
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(5000)]
        public string Content { get; set; } = string.Empty;

        public DateTime DateTimeCreated { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }

        [NotMapped]
        public string UserDisplayName { get; set; } = string.Empty;

        public int WorkoutId { get; set; }

        [NotMapped]
        public string WorkoutTitle { get; set; } = string.Empty;
    }
}
