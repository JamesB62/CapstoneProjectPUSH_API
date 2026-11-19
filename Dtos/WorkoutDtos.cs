namespace CapstoneProjectPUSH_API.Dtos
{
    // What the app sends to create or edit a workout, with its exercises
    public class WorkoutRequest
    {
        public string Title { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public bool IsPrivate { get; set; } = true;
        public List<WorkoutExerciseRequest> Exercises { get; set; } = new List<WorkoutExerciseRequest>();
    }

    // One exercise inside a workout
    public class WorkoutExerciseRequest
    {
        public int ExerciseId { get; set; }
        public int SortOrder { get; set; }
        public int Sets { get; set; }
        public int Reps { get; set; }
        public int DurationMinutes { get; set; }
    }

    // Reply after creating a workout
    public class WorkoutCreatedResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int WorkoutId { get; set; }
    }

    // Reply after toggling a favourite
    public class FavouriteResponse
    {
        public bool Success { get; set; }
        public bool IsFavourite { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
