namespace BookApi.Api.Services;

public interface IImageStorageService
{
    bool IsSupported(IFormFile? formFile);

    Task<string?> SaveImageAsync(IFormFile? formFile, CancellationToken cancellationToken = default);

    void DeleteImage(string? relativePath);
}
