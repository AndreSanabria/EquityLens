using EquityLens.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EquityLens.Api.Data;

public class EquityLensDbContext(DbContextOptions<EquityLensDbContext> options) : DbContext(options)
{
    public DbSet<WatchlistItem> WatchlistItems => Set<WatchlistItem>();
    public DbSet<ResearchSnapshot> ResearchSnapshots => Set<ResearchSnapshot>();
    public DbSet<ApiRequestLog> ApiRequestLogs => Set<ApiRequestLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WatchlistItem>()
            .HasIndex(item => item.Ticker)
            .IsUnique();

        modelBuilder.Entity<WatchlistItem>()
            .Property(item => item.Ticker)
            .HasMaxLength(10);

        modelBuilder.Entity<ResearchSnapshot>()
            .Property(snapshot => snapshot.Ticker)
            .HasMaxLength(10);

        modelBuilder.Entity<ApiRequestLog>()
            .Property(log => log.Ticker)
            .HasMaxLength(10);
    }
}
