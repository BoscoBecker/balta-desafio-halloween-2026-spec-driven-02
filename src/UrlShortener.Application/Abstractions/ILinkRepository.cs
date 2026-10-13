using UrlShortener.Domain;

namespace UrlShortener.Application.Abstractions;

public interface ILinkRepository
{
    Task<Link?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string code, CancellationToken cancellationToken);
    Task AddAsync(Link link, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<int> IncrementAccessCountAsync(string code, DateTime utcNow, CancellationToken cancellationToken);
}
