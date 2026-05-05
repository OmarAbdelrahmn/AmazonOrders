namespace EmployeeOrderApi.Services.Image;

public interface IImageService
{
    Task<string?> SaveImageAsync(IFormFile file, string folder = "images");
    void          DeleteImage(string? relativePath);
}

public sealed class ImageService(IWebHostEnvironment env) : IImageService
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public async Task<string?> SaveImageAsync(IFormFile file, string folder = "images")
    {
        if (file is null || file.Length == 0)
            return null;

        if (file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("File size exceeds the 5 MB limit.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"Extension '{ext}' is not allowed. Use jpg, jpeg, png, or webp.");

        var uploadsDir = Path.Combine(env.WebRootPath, folder);
        Directory.CreateDirectory(uploadsDir);

        var fileName  = $"{Guid.NewGuid()}{ext}";
        var fullPath  = Path.Combine(uploadsDir, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        // Return relative URL that can be served statically
        return $"/{folder}/{fileName}";
    }

    public void DeleteImage(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return;

        // Strip leading slash and combine with wwwroot
        var safeRelative = relativePath.TrimStart('/');
        var fullPath = Path.Combine(env.WebRootPath, safeRelative);

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
