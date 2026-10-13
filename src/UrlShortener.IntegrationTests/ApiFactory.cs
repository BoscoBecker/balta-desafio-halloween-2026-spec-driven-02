using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Application;

namespace UrlShortener.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"urlshortener-{Guid.NewGuid():N}.db");

    public MutableClock Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath};Pooling=False");
        builder.ConfigureServices(services =>
        {
            var existing = services.Single(descriptor => descriptor.ServiceType == typeof(IClock));
            services.Remove(existing);
            services.AddSingleton<IClock>(Clock);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
        {
            return;
        }

        foreach (var suffix in new[] { "", "-shm", "-wal" })
        {
            var path = _dbPath + suffix;
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
                // SQLite can keep a short lock after the host shuts down.
            }
        }
    }
}

public sealed class MutableClock : IClock
{
    public DateTime UtcNow { get; set; } = DateTime.UtcNow;
}
