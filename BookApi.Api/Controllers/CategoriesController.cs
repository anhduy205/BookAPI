using BookApi.Api.Contracts;
using BookApi.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BookApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoriesController(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        var response = categories
            .Select(category => new CategoryResponse
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName
            })
            .ToList();

        return Ok(response);
    }
}
