namespace BookApi.Api.Contracts;

public sealed class BookResponse
{
    public int BookId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Author { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public int Quantity { get; init; }

    public string? Description { get; init; }

    public int CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public string? ImagePath { get; init; }

    public string? ImageUrl { get; init; }
}
