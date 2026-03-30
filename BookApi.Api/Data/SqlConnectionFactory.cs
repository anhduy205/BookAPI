using System.Data.SqlClient;

namespace BookApi.Api.Data;

public interface ISqlConnectionFactory
{
    string DatabaseName { get; }

    Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);

    Task<SqlConnection> OpenMasterConnectionAsync(CancellationToken cancellationToken = default);
}

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly IReadOnlyList<string> _connectionStrings;
    private readonly IReadOnlyList<string> _masterConnectionStrings;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        var configuredConnectionString = configuration.GetConnectionString("BookStoreDb")
            ?? throw new InvalidOperationException("ConnectionStrings:BookStoreDb is missing.");

        var primaryBuilder = new SqlConnectionStringBuilder(configuredConnectionString)
        {
            ConnectTimeout = 3
        };
        DatabaseName = primaryBuilder.InitialCatalog;

        var candidates = new List<string> { primaryBuilder.ConnectionString };
        foreach (var dataSource in BuildFallbackDataSources(primaryBuilder.DataSource))
        {
            var fallbackBuilder = new SqlConnectionStringBuilder(primaryBuilder.ConnectionString)
            {
                DataSource = dataSource,
                ConnectTimeout = 3
            };

            candidates.Add(fallbackBuilder.ConnectionString);
        }

        _connectionStrings = candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        _masterConnectionStrings = _connectionStrings
            .Select(connectionString =>
            {
                var masterBuilder = new SqlConnectionStringBuilder(connectionString)
                {
                    InitialCatalog = "master",
                    ConnectTimeout = 3
                };
                return masterBuilder.ConnectionString;
            })
            .ToList();
    }

    public string DatabaseName { get; }

    public Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        return OpenAnyAsync(_connectionStrings, cancellationToken);
    }

    public Task<SqlConnection> OpenMasterConnectionAsync(CancellationToken cancellationToken = default)
    {
        return OpenAnyAsync(_masterConnectionStrings, cancellationToken);
    }

    private static IEnumerable<string> BuildFallbackDataSources(string primaryDataSource)
    {
        var fallbacks = new[]
        {
            @".\SQLEXPRESS",
            @"DESKTOP-AGCJK63\SQLEXPRESS",
            @"localhost\SQLEXPRESS",
            @".\SQLEXPRESS01",
            @"DESKTOP-AGCJK63\SQLEXPRESS01",
            @"localhost\SQLEXPRESS01",
            @"127.0.0.1,1433",
            @"127.0.0.1,14330"
        };

        foreach (var fallback in fallbacks)
        {
            if (!string.Equals(primaryDataSource, fallback, StringComparison.OrdinalIgnoreCase))
            {
                yield return fallback;
            }
        }
    }

    private static async Task<SqlConnection> OpenAnyAsync(
        IEnumerable<string> connectionStrings,
        CancellationToken cancellationToken)
    {
        var failures = new List<Exception>();

        foreach (var connectionString in connectionStrings)
        {
            var connection = new SqlConnection(connectionString);
            try
            {
                await connection.OpenAsync(cancellationToken);
                return connection;
            }
            catch (Exception ex)
            {
                failures.Add(new InvalidOperationException(
                    $"Failed to connect using '{new SqlConnectionStringBuilder(connectionString).DataSource}'.",
                    ex));
                connection.Dispose();
            }
        }

        throw new AggregateException(
            "Unable to connect to SQL Server using the configured BookStoreDb connection candidates.",
            failures);
    }
}
