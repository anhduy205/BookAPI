namespace BookApi.WinForms.Models;

public sealed class CategoryItem
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;
}

public sealed class CategoryOption
{
    public int? CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public override string ToString()
    {
        return CategoryName;
    }
}

public sealed class BookItem
{
    public int BookId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public string? Description { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    public string? ImageUrl { get; set; }
}

public sealed class BookFormModel
{
    public string Title { get; init; } = string.Empty;

    public string Author { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public int Quantity { get; init; }

    public string? Description { get; init; }

    public int CategoryId { get; init; }
}
