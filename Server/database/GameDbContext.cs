using Microsoft.EntityFrameworkCore;
using MoonlightFarm.Server.Models;

namespace MoonlightFarm.Server.Database
{
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

        public DbSet<SavedGame> SavedGames { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SavedGame>()
                .HasKey(s => s.SessionId);
            
            // Storing GameState as JSON in SQLite
            modelBuilder.Entity<SavedGame>()
                .Property(s => s.Data)
                .IsRequired();
        }
    }

    public class SavedGame
    {
        public string SessionId { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public DateTime LastSaved { get; set; } = DateTime.UtcNow;
    }
}
