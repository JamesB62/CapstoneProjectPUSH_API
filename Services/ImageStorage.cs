namespace CapstoneProjectPUSH_API.Services
{
    // Saves uploaded photos in the "uploads" folder on the server
    public class ImageStorage
    {
        public const string RequestPath = "/uploads";

        private const long MaxBytes = 5 * 1024 * 1024;

        private static readonly string[] AllowedExtensions = new string[] { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly string _uploadsFolder;
        private readonly ILogger<ImageStorage> _logger;

        public ImageStorage(IWebHostEnvironment environment, ILogger<ImageStorage> logger)
        {
            _uploadsFolder = GetUploadsFolder(environment);
            _logger = logger;
        }

        // Defines the upload location -- shared with Program.cs so both use the same folder
        public static string GetUploadsFolder(IWebHostEnvironment environment)
        {
            return Path.Combine(environment.ContentRootPath, "uploads");
        }

        // Validate image file -- returns an error message, or null if the file is fine
        public static string? Validate(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return "Please choose a photo.";
            }

            if (file.Length > MaxBytes)
            {
                return "That photo is too large (5 MB max).";
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
            {
                return "That photo couldn't be used. Please choose a JPG, PNG or WebP image.";
            }

            return null;
        }

        // Saves the image with a new random name and returns its path
        public async Task<string> SaveAsync(IFormFile file)
        {
            Directory.CreateDirectory(_uploadsFolder);

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string fileName = Guid.NewGuid().ToString() + extension;
            string fullPath = Path.Combine(_uploadsFolder, fileName);

            using (FileStream destination = File.Create(fullPath))
            {
                await file.CopyToAsync(destination);
            }

            return RequestPath + "/" + fileName;
        }

        // Deletes a photo
        public void Delete(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            if (!imageUrl.StartsWith(RequestPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Only the file name is used, so a path can't point outside the uploads folder
            string fileName = Path.GetFileName(imageUrl);
            string fullPath = Path.Combine(_uploadsFolder, fileName);

            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Couldn't delete image {ImageUrl}", imageUrl);
            }
        }
    }
}