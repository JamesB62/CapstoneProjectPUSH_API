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
}