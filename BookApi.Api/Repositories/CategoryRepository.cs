using System.Data;
using Microsoft.Data.SqlClient;
using BookApi.Api.Data;
using BookApi.Api.Models;

namespace BookApi.Api.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public CategoryRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<CategoryRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT CategoryId, CategoryName
                           FROM dbo.Categories
                           ORDER BY CategoryName;
                           """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var categories = new List<CategoryRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            categories.Add(new CategoryRecord
            {
                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                CategoryName = reader.GetString(reader.GetOrdinal("CategoryName"))
            });
        }

        return categories;
    }

    public async Task<bool> ExistsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT COUNT(1)
                           FROM dbo.Categories
                           WHERE CategoryId = @CategoryId;
                           """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@CategoryId", SqlDbType.Int).Value = categoryId;

        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return count > 0;
    }
}
