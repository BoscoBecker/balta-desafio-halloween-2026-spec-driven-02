using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Application;
using UrlShortener.Application.Abstractions;
using UrlShortener.Domain;

namespace UrlShortener.Infrastructure;

public sealed class LinkRepository(AppDbContext db) : ILinkRepository
{
    public Task<Link?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        db.Links.AsNoTracking().FirstOrDefaultAsync(link => link.Code == code, cancellationToken);

    public Task<bool> ExistsAsync(string code, CancellationToken cancellationToken) =>
        db.Links.AnyAsync(link => link.Code == code, cancellationToken);

    public async Task AddAsync(Link link, CancellationToken cancellationToken) =>
        await db.Links.AddAsync(link, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraint(exception))
        {
            var code = db.ChangeTracker
                .Entries<Link>()
                .Select(entry => entry.Entity.Code)
                .FirstOrDefault() ?? string.Empty;

            db.ChangeTracker.Clear();
            throw new DuplicateCodeException(code);
        }
    }

    public Task<int> IncrementAccessCountAsync(string code, DateTime utcNow, CancellationToken cancellationToken) =>
        db.Links
            .Where(link =>
                link.Code == code &&
                (link.ExpiresAtUtc == null || link.ExpiresAtUtc >= utcNow))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(link => link.AccessCount, link => link.AccessCount + 1),
                cancellationToken);

    private static bool IsUniqueConstraint(DbUpdateException exception) =>
        exception.InnerException is SqliteException sqlite &&
        sqlite.SqliteErrorCode == 19;
}
