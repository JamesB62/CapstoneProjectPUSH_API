namespace CapstoneProjectPUSH_API.Dtos
{
    // Simple success/message reply, like the app's (bool Success, string Message) results
    public class MessageResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public MessageResponse()
        {
        }

        public MessageResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }

    // What the app sends to add or edit a gym
    public class GymRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }
}
