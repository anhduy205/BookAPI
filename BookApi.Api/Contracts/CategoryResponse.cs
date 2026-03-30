namespace BookApi.Api.Contracts;

public sealed class CategoryResponse
{
    public int CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;
}
