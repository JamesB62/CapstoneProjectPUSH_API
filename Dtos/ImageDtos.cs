namespace CapstoneProjectPUSH_API.Dtos
{
    // Reply after uploading or removing a photo
    public class ImageResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }
}