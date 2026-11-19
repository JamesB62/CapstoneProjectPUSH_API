using System.ComponentModel.DataAnnotations.Schema;

namespace CapstoneProjectPUSH_API.Models
{
    public class WorkoutExercise
    {
        public int Id { get; set; }

        public int Reps { get; set; }

        public int Sets { get; set; }

        public int DurationMinutes { get; set; }

        public int SortOrder { get; set; }

        public int WorkoutId { get; set; }

        [NotMapped]
        public string WorkoutTitle { get; set; } = string.Empty;

        public int ExerciseId { get; set; }

        [NotMapped]
        public string ExerciseTitle { get; set; } = string.Empty;
    }
}
