using BookApi.Api.Models;

namespace BookApi.Api.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<CategoryRecord>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int categoryId, CancellationToken cancellationToken = default);
}
