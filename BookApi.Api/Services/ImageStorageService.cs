namespace BookApi.Api.Services;

public sealed class ImageStorageService : IImageStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private readonly string _imageDirectory;
    private readonly string _contentRootPath;

    public ImageStorageService(IWebHostEnvironment environment)
    {
        _contentRootPath = environment.ContentRootPath;
        _imageDirectory = Path.Combine(_contentRootPath, "Content", "ImageBooks");
        Directory.CreateDirectory(_imageDirectory);
    }

    public bool IsSupported(IFormFile? formFile)
    {
        if (formFile is null || string.IsNullOrWhiteSpace(formFile.FileName))
        {
            return true;
        }

        return AllowedExtensions.Contains(Path.GetExtension(formFile.FileName));
    }

    public async Task<string?> SaveImageAsync(IFormFile? formFile, CancellationToken cancellationToken = default)
    {
        if (formFile is null || formFile.Length == 0)
        {
            return null;
        }

        if (!IsSupported(formFile))
        {
            throw new InvalidOperationException("Only .jpg, .jpeg, .png, and .webp images are supported.");
        }

        var extension = Path.GetExtension(formFile.FileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(_imageDirectory, fileName);

        await using var stream = File.Create(physicalPath);
        await formFile.CopyToAsync(stream, cancellationToken);

        return Path.Combine("Content", "ImageBooks", fileName).Replace("\\", "/", StringComparison.Ordinal);
    }

    public void DeleteImage(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        var normalizedPath = relativePath.Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        var physicalPath = Path.Combine(_contentRootPath, normalizedPath);

        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }
    }
}
