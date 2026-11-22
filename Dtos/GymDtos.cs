namespace CapstoneProjectPUSH_API.Dtos
{
    // What the app sends to add or edit a gym
    public class GymRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }
}