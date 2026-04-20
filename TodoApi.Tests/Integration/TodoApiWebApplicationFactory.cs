using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TodoApi.Options;

namespace TodoApi.Tests.Integration;

public class TodoApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath;

    public TodoApiWebApplicationFactory()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"todo_api_test_{Guid.NewGuid():N}.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{TodoDatabaseOptions.SectionName}:ConnectionString"] = $"Data Source={_dbPath}"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            TryDelete(_dbPath);
        }

        base.Dispose(disposing);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup; SQLite may still hold a lock briefly on Windows.
        }
    }
}
