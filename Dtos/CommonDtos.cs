namespace CapstoneProjectPUSH_API.Dtos
{
    // Simple success/message reply
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

    // Reply after adding something that the app needs the new Id of
    public class CreatedResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Id { get; set; }
    }
}