namespace CapstoneProjectPUSH_API.Dtos
{
    // What the app sends to add or edit an exercise (the photo is handled separately)
    public class ExerciseRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsTimed { get; set; }
        public int Difficulty { get; set; } = 1;
        public int CategoryId { get; set; }
    }
}
