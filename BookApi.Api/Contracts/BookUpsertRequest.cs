using System.ComponentModel.DataAnnotations;

namespace BookApi.Api.Contracts;

public sealed class BookUpsertRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Author { get; init; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Price { get; init; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; init; }

    [StringLength(1000)]
    public string? Description { get; init; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }

    public IFormFile? ImageFile { get; init; }
}
