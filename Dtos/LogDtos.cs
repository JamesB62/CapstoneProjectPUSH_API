namespace CapstoneProjectPUSH_API.Dtos
{
    // What the app sends when a workout is finished
    public class WorkoutLogRequest
    {
        public int WorkoutId { get; set; }
        public int DurationSeconds { get; set; }
    }

    // What the app sends to save today's weight
    public class WeightLogRequest
    {
        public double WeightKg { get; set; }
    }
}