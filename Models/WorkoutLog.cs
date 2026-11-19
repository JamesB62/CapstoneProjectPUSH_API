using System.ComponentModel.DataAnnotations.Schema;

namespace CapstoneProjectPUSH_API.Models
{
    public class WorkoutLog
    {
        public int Id { get; set; }

        public int DurationSeconds { get; set; }

        public DateTime DateTimeCompleted { get; set; } = DateTime.UtcNow;

        public int WorkoutId { get; set; }

        [NotMapped]
        public string WorkoutTitle { get; set; } = string.Empty;

        public int UserId { get; set; }

        [NotMapped]
        public string UserDisplayName { get; set; } = string.Empty;
    }
}
