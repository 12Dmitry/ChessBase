using Microsoft.EntityFrameworkCore;

namespace ChessBase.Data;

public class ChessDbContext : DbContext
{
    public DbSet<Game> Games { get; set; }
    public DbSet<MoveAnalysis> Moves { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseNpgsql("Host=localhost;Database=chess_db;Username=postgres;Password=0000"); //todo extend to config

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Индекс для предотвращения дубликатов партий [2, 3]
        modelBuilder.Entity<Game>()
            .HasIndex(g => g.ExternalId)
            .IsUnique();
    }
}