using BookApi.Api.Contracts;
using BookApi.Api.Models;

namespace BookApi.Api.Services;

public static class BookResponseMapper
{
    public static BookResponse ToResponse(this BookRecord record, HttpRequest request)
    {
        string? imageUrl = null;
        if (!string.IsNullOrWhiteSpace(record.ImagePath))
        {
            var normalizedPath = record.ImagePath.Replace("\\", "/", StringComparison.Ordinal);
            imageUrl = $"{request.Scheme}://{request.Host}/{normalizedPath}";
        }

        return new BookResponse
        {
            BookId = record.BookId,
            Title = record.Title,
            Author = record.Author,
            Price = record.Price,
            Quantity = record.Quantity,
            Description = record.Description,
            CategoryId = record.CategoryId,
            CategoryName = record.CategoryName,
            ImagePath = record.ImagePath,
            ImageUrl = imageUrl
        };
    }
}
