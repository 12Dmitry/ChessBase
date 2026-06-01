using Microsoft.EntityFrameworkCore;

namespace ChessBase.Data;

public class ChessDbContext : DbContext
{
    public ChessDbContext(DbContextOptions<ChessDbContext> options) : base(options)
    {
    }
    public DbSet<GameReport> Games { get; set; }
    public DbSet<MoveAnalysis> Moves { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameReport>()
            .HasIndex(g => g.ExternalId)
            .IsUnique();
    }
}