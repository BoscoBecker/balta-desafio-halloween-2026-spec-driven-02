using Microsoft.EntityFrameworkCore;
using UrlShortener.Domain;

namespace UrlShortener.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Link> Links => Set<Link>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var link = modelBuilder.Entity<Link>();
        link.ToTable("Links");
        link.HasKey(x => x.Id);
        link.Property(x => x.Code).IsRequired().HasMaxLength(20);
        link.HasIndex(x => x.Code).IsUnique();
        link.Property(x => x.TargetUrl).IsRequired().HasMaxLength(2048);
        link.Property(x => x.ManagementKeyHash).IsRequired().HasMaxLength(64);
        link.Property(x => x.AccessCount).IsRequired();
        link.Property(x => x.CreatedAtUtc).IsRequired();
    }
}
