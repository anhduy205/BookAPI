using System.Data.SqlClient;

namespace BookApi.Api.Data;

public sealed class DatabaseInitializer
{
    private static readonly string[] SeedCategories =
    [
        "Novel",
        "Science",
        "Technology",
        "History",
        "Children"
    ];

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        ISqlConnectionFactory connectionFactory,
        ILogger<DatabaseInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseAsync(cancellationToken);
        await EnsureTablesAsync(cancellationToken);
    }

    private async Task EnsureDatabaseAsync(CancellationToken cancellationToken)
    {
        var databaseName = _connectionFactory.DatabaseName;
        var escapedDatabaseName = databaseName.Replace("'", "''", StringComparison.Ordinal);
        var safeDatabaseName = $"[{databaseName.Replace("]", "]]", StringComparison.Ordinal)}]";
        var sql = $"""
                   IF DB_ID(N'{escapedDatabaseName}') IS NULL
                   BEGIN
                       EXEC('CREATE DATABASE {safeDatabaseName}')
                   END
                   """;

        using var connection = await _connectionFactory.OpenMasterConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Database '{DatabaseName}' is ready.", databaseName);
    }

    private async Task EnsureTablesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
                           IF OBJECT_ID('dbo.Categories', 'U') IS NULL
                           BEGIN
                               CREATE TABLE dbo.Categories
                               (
                                   CategoryId INT IDENTITY(1,1) PRIMARY KEY,
                                   CategoryName NVARCHAR(100) NOT NULL UNIQUE
                               );
                           END;

                           IF OBJECT_ID('dbo.Books', 'U') IS NULL
                           BEGIN
                               CREATE TABLE dbo.Books
                               (
                                   BookId INT IDENTITY(1,1) PRIMARY KEY,
                                   Title NVARCHAR(200) NOT NULL,
                                   Author NVARCHAR(150) NOT NULL,
                                   Price DECIMAL(18,2) NOT NULL,
                                   Quantity INT NOT NULL,
                                   Description NVARCHAR(1000) NULL,
                                   ImagePath NVARCHAR(255) NULL,
                                   CategoryId INT NOT NULL,
                                   CONSTRAINT FK_Books_Categories
                                       FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId)
                               );
                           END;
                           """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        using (var command = new SqlCommand(sql, connection))
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var categoryName in SeedCategories)
        {
            using var seedCommand = new SqlCommand(
                """
                IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryName = @CategoryName)
                BEGIN
                    INSERT INTO dbo.Categories (CategoryName)
                    VALUES (@CategoryName);
                END
                """,
                connection);

            seedCommand.Parameters.AddWithValue("@CategoryName", categoryName);
            await seedCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        _logger.LogInformation("Tables and seed data are ready.");
    }
}
