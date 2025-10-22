using System.IO;

namespace MoviesApi.Services
{
    public interface IFileUploadService
    {
        Task<string?> SaveImageAsync(IFormFile? imageFile);
        void DeleteImage(string? imagePath);
    }

    public class FileUploadService : IFileUploadService
    {
        private readonly string _uploadsFolder;
        private readonly ILogger<FileUploadService> _logger;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

        public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
        {
            _uploadsFolder = Path.Combine(environment.WebRootPath ?? environment.ContentRootPath, "uploads", "images");
            _logger = logger;
            
            // Ensure the uploads directory exists
            Directory.CreateDirectory(_uploadsFolder);
        }

        public async Task<string?> SaveImageAsync(IFormFile? imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            // Validate file size
            if (imageFile.Length > MaxFileSizeBytes)
            {
                throw new InvalidOperationException($"File size exceeds maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)}MB");
            }

            // Validate file extension
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException($"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}");
            }

            try
            {
                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(_uploadsFolder, fileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Return relative path for storage in database
                return $"/uploads/images/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving image file {FileName}", imageFile.FileName);
                throw new InvalidOperationException("Failed to save image file", ex);
            }
        }

        public void DeleteImage(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return;

            try
            {
                // Convert relative path to absolute path
                var fileName = Path.GetFileName(imagePath);
                var fullPath = Path.Combine(_uploadsFolder, fileName);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("Deleted image file {FilePath}", fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete image file {ImagePath}", imagePath);
                // Don't throw - deletion failure shouldn't break the operation
            }
        }
    }
}