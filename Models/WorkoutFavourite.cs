namespace CapstoneProjectPUSH_API.Models
{
    // A workout a user has favourited (a user can only favourite the same workout once)
    public class WorkoutFavourite
    {
        public int Id { get; set; }

        public DateTime DateTimeAdded { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }

        public int WorkoutId { get; set; }
    }
}
