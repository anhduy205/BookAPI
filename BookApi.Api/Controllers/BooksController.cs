using BookApi.Api.Contracts;
using BookApi.Api.Models;
using BookApi.Api.Repositories;
using BookApi.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BooksController : ControllerBase
{
    private readonly IBookRepository _bookRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IImageStorageService _imageStorageService;

    public BooksController(
        IBookRepository bookRepository,
        ICategoryRepository categoryRepository,
        IImageStorageService imageStorageService)
    {
        _bookRepository = bookRepository;
        _categoryRepository = categoryRepository;
        _imageStorageService = imageStorageService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookResponse>>> SearchAsync(
        [FromQuery] string? keyword,
        [FromQuery] int? categoryId,
        CancellationToken cancellationToken)
    {
        var books = await _bookRepository.SearchAsync(keyword, categoryId, cancellationToken);
        return Ok(books.Select(book => book.ToResponse(Request)).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var book = await _bookRepository.GetByIdAsync(id, cancellationToken);
        if (book is null)
        {
            return NotFound(new { message = "Book not found." });
        }

        return Ok(book.ToResponse(Request));
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BookResponse>> CreateAsync(
        [FromForm] BookUpsertRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateRequestAsync(request, cancellationToken);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        string? savedImagePath = null;

        try
        {
            savedImagePath = await _imageStorageService.SaveImageAsync(request.ImageFile, cancellationToken);
            var bookId = await _bookRepository.CreateAsync(
                new BookMutationModel
                {
                    Title = request.Title,
                    Author = request.Author,
                    Price = request.Price,
                    Quantity = request.Quantity,
                    Description = request.Description,
                    CategoryId = request.CategoryId,
                    ImagePath = savedImagePath
                },
                cancellationToken);

            var createdBook = await _bookRepository.GetByIdAsync(bookId, cancellationToken);
            return CreatedAtAction(nameof(GetByIdAsync), new { id = bookId }, createdBook!.ToResponse(Request));
        }
        catch
        {
            _imageStorageService.DeleteImage(savedImagePath);
            throw;
        }
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BookResponse>> UpdateAsync(
        int id,
        [FromForm] BookUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var existingBook = await _bookRepository.GetByIdAsync(id, cancellationToken);
        if (existingBook is null)
        {
            return NotFound(new { message = "Book not found." });
        }

        await ValidateRequestAsync(request, cancellationToken);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        string? savedImagePath = null;
        var finalImagePath = existingBook.ImagePath;

        try
        {
            if (request.ImageFile is not null && request.ImageFile.Length > 0)
            {
                savedImagePath = await _imageStorageService.SaveImageAsync(request.ImageFile, cancellationToken);
                finalImagePath = savedImagePath;
            }

            var updated = await _bookRepository.UpdateAsync(
                id,
                new BookMutationModel
                {
                    Title = request.Title,
                    Author = request.Author,
                    Price = request.Price,
                    Quantity = request.Quantity,
                    Description = request.Description,
                    CategoryId = request.CategoryId,
                    ImagePath = finalImagePath
                },
                cancellationToken);

            if (!updated)
            {
                if (savedImagePath is not null)
                {
                    _imageStorageService.DeleteImage(savedImagePath);
                }

                return NotFound(new { message = "Book not found." });
            }

            if (savedImagePath is not null && !string.Equals(existingBook.ImagePath, savedImagePath, StringComparison.Ordinal))
            {
                _imageStorageService.DeleteImage(existingBook.ImagePath);
            }

            var updatedBook = await _bookRepository.GetByIdAsync(id, cancellationToken);
            return Ok(updatedBook!.ToResponse(Request));
        }
        catch
        {
            if (savedImagePath is not null)
            {
                _imageStorageService.DeleteImage(savedImagePath);
            }

            throw;
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var existingBook = await _bookRepository.GetByIdAsync(id, cancellationToken);
        if (existingBook is null)
        {
            return NotFound(new { message = "Book not found." });
        }

        var deleted = await _bookRepository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = "Book not found." });
        }

        _imageStorageService.DeleteImage(existingBook.ImagePath);
        return NoContent();
    }

    private async Task ValidateRequestAsync(BookUpsertRequest request, CancellationToken cancellationToken)
    {
        if (!_imageStorageService.IsSupported(request.ImageFile))
        {
            ModelState.AddModelError(nameof(request.ImageFile), "Only .jpg, .jpeg, .png, and .webp images are supported.");
        }

        var categoryExists = await _categoryRepository.ExistsAsync(request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            ModelState.AddModelError(nameof(request.CategoryId), "The selected category does not exist.");
        }
    }
}
