using System.Data;
using Microsoft.Data.SqlClient;
using BookApi.Api.Data;
using BookApi.Api.Models;

namespace BookApi.Api.Repositories;

public sealed class BookRepository : IBookRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public BookRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<BookRecord>> SearchAsync(
        string? keyword,
        int? categoryId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT
                               b.BookId,
                               b.Title,
                               b.Author,
                               b.Price,
                               b.Quantity,
                               b.Description,
                               b.ImagePath,
                               b.CategoryId,
                               c.CategoryName
                           FROM dbo.Books AS b
                           INNER JOIN dbo.Categories AS c ON c.CategoryId = b.CategoryId
                           WHERE
                               (
                                   @Keyword = N'' OR
                                   b.Title LIKE N'%' + @Keyword + N'%' OR
                                   b.Author LIKE N'%' + @Keyword + N'%' OR
                                   c.CategoryName LIKE N'%' + @Keyword + N'%'
                               )
                               AND (@CategoryId IS NULL OR b.CategoryId = @CategoryId)
                           ORDER BY b.BookId DESC;
                           """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Keyword", SqlDbType.NVarChar, 200).Value = keyword?.Trim() ?? string.Empty;
        command.Parameters.Add("@CategoryId", SqlDbType.Int).Value = categoryId is null ? DBNull.Value : categoryId.Value;

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var books = new List<BookRecord>();

        while (await reader.ReadAsync(cancellationToken))
        {
            books.Add(MapBook(reader));
        }

        return books;
    }

    public async Task<BookRecord?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT
                               b.BookId,
                               b.Title,
                               b.Author,
                               b.Price,
                               b.Quantity,
                               b.Description,
                               b.ImagePath,
                               b.CategoryId,
                               c.CategoryName
                           FROM dbo.Books AS b
                           INNER JOIN dbo.Categories AS c ON c.CategoryId = b.CategoryId
                           WHERE b.BookId = @BookId;
                           """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@BookId", SqlDbType.Int).Value = id;

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapBook(reader);
    }

    public async Task<int> CreateAsync(BookMutationModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO dbo.Books
                           (
                               Title,
                               Author,
                               Price,
                               Quantity,
                               Description,
                               ImagePath,
                               CategoryId
                           )
                           VALUES
                           (
                               @Title,
                               @Author,
                               @Price,
                               @Quantity,
                               @Description,
                               @ImagePath,
                               @CategoryId
                           );

                           SELECT CAST(SCOPE_IDENTITY() AS INT);
                           """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        using var command = BuildMutationCommand(sql, connection, model);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<bool> UpdateAsync(int id, BookMutationModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           UPDATE dbo.Books
                           SET
                               Title = @Title,
                               Author = @Author,
                               Price = @Price,
                               Quantity = @Quantity,
                               Description = @Description,
                               ImagePath = @ImagePath,
                               CategoryId = @CategoryId
                           WHERE BookId = @BookId;
                           """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        using var command = BuildMutationCommand(sql, connection, model);
        command.Parameters.Add("@BookId", SqlDbType.Int).Value = id;

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Books WHERE BookId = @BookId;";

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@BookId", SqlDbType.Int).Value = id;

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        return affectedRows > 0;
    }

    private static SqlCommand BuildMutationCommand(string sql, SqlConnection connection, BookMutationModel model)
    {
        var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = model.Title.Trim();
        command.Parameters.Add("@Author", SqlDbType.NVarChar, 150).Value = model.Author.Trim();
        var priceParameter = command.Parameters.Add("@Price", SqlDbType.Decimal);
        priceParameter.Precision = 18;
        priceParameter.Scale = 2;
        priceParameter.Value = model.Price;
        command.Parameters.Add("@Quantity", SqlDbType.Int).Value = model.Quantity;
        command.Parameters.Add("@Description", SqlDbType.NVarChar, 1000).Value =
            string.IsNullOrWhiteSpace(model.Description) ? DBNull.Value : model.Description.Trim();
        command.Parameters.Add("@ImagePath", SqlDbType.NVarChar, 255).Value =
            string.IsNullOrWhiteSpace(model.ImagePath) ? DBNull.Value : model.ImagePath;
        command.Parameters.Add("@CategoryId", SqlDbType.Int).Value = model.CategoryId;
        return command;
    }

    private static BookRecord MapBook(SqlDataReader reader)
    {
        return new BookRecord
        {
            BookId = reader.GetInt32(reader.GetOrdinal("BookId")),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            Author = reader.GetString(reader.GetOrdinal("Author")),
            Price = reader.GetDecimal(reader.GetOrdinal("Price")),
            Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                ? null
                : reader.GetString(reader.GetOrdinal("Description")),
            ImagePath = reader.IsDBNull(reader.GetOrdinal("ImagePath"))
                ? null
                : reader.GetString(reader.GetOrdinal("ImagePath")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            CategoryName = reader.GetString(reader.GetOrdinal("CategoryName"))
        };
    }
}
