using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Application.Abstractions;

namespace UrlShortener.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<ILinkRepository, LinkRepository>();
        services.AddSingleton<IManagementKeyService, ManagementKeyService>();
        services.AddSingleton<ILinkRedirectCache, MemoryLinkRedirectCache>();
        return services;
    }
}
