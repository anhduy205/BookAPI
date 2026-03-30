using BookApi.Api.Models;

namespace BookApi.Api.Repositories;

public interface IBookRepository
{
    Task<IReadOnlyList<BookRecord>> SearchAsync(
        string? keyword,
        int? categoryId,
        CancellationToken cancellationToken = default);

    Task<BookRecord?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(BookMutationModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(int id, BookMutationModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
