using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace ECommerce.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the real API in-process, overriding only the configuration needed for tests
/// (an isolated SQLite database) — never recreates the application pipeline manually.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"ecommerce-tests-{Guid.NewGuid():N}.db");

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Database", $"Data Source={_databasePath}");
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // The Sqlite connection pool keeps a handle open after the host shuts down;
        // clear it first or the file remains locked and deletion throws.
        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }
}
