namespace CapstoneProjectPUSH_API.Dtos
{
    // What the app sends to add or edit a trainer (the photo is handled separately)
    public class TrainerRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Available { get; set; } = true;
        public int CategoryId { get; set; }
        public int GymId { get; set; }
    }
}
