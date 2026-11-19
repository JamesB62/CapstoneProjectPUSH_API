namespace CapstoneProjectPUSH_API.Dtos
{
    // What the app sends to report a workout
    public class ReportRequest
    {
        public int WorkoutId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
