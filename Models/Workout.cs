using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapstoneProjectPUSH_API.Models
{
    public class Workout
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [NotMapped]
        public int ExerciseCount { get; set; }

        [NotMapped]
        public int SetCount { get; set; }

        [NotMapped]
        public int TotalMinutes { get; set; }

        public DateTime DateTimeCreated { get; set; } = DateTime.UtcNow;

        public bool IsPrivate { get; set; } = true;

        // Set per user by the API (not stored on the workout)
        [NotMapped]
        public bool IsFavourite { get; set; }

        public int UserId { get; set; }

        [NotMapped]
        public string UserDisplayName { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        [NotMapped]
        public string CategoryTitle { get; set; } = string.Empty;
    }
}
