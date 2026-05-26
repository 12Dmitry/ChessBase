using Microsoft.EntityFrameworkCore;

namespace ChessBase.Data;

public class ChessDbContext : DbContext
{
    public DbSet<Game> Games { get; set; }
    public DbSet<MoveAnalysis> Moves { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>()
            .HasIndex(g => g.ExternalId)
            .IsUnique();
    }
}