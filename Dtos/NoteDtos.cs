namespace CapstoneProjectPUSH_API.Dtos
{
    // What the app sends to add a note
    public class NoteRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int WorkoutId { get; set; }
    }

    // Reply after adding a note
    public class NoteCreatedResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int NoteId { get; set; }
    }
}