using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace MoonlightFarm.Server.Services
{
    public class PersistenceManager
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PersistenceManager> _logger;

        public PersistenceManager(IServiceScopeFactory scopeFactory, ILogger<PersistenceManager> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            
            // Ensure database is created and migrated
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            context.Database.EnsureCreated();
        }

        public async Task SaveGameAsync(string sessionId, GameState state)
        {
            state.LastSavedAt = DateTime.UtcNow.Ticks;
            var json = JsonSerializer.Serialize(state);

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();

            var existing = await context.SavedGames.FindAsync(sessionId);
            if (existing != null)
            {
                existing.Data = json;
                existing.LastSaved = DateTime.UtcNow;
            }
            else
            {
                await context.SavedGames.AddAsync(new SavedGame
                {
                    SessionId = sessionId,
                    Data = json,
                    LastSaved = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }

        public async Task<GameState?> LoadGameAsync(string sessionId)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();

            var saved = await context.SavedGames.FindAsync(sessionId);
            if (saved == null) return null;

            try
            {
                return JsonSerializer.Deserialize<GameState>(saved.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deserializing game state for session {sessionId}");
                return null;
            }
        }

        public bool SaveExists(string sessionId)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            return context.SavedGames.Any(s => s.SessionId == sessionId);
        }

        public async Task SaveAllAsync(IEnumerable<(string Id, GameState State)> sessions)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();

            foreach (var (id, state) in sessions)
            {
                var json = JsonSerializer.Serialize(state);
                var existing = await context.SavedGames.FindAsync(id);
                if (existing != null)
                {
                    existing.Data = json;
                    existing.LastSaved = DateTime.UtcNow;
                }
                else
                {
                    await context.SavedGames.AddAsync(new SavedGame { SessionId = id, Data = json });
                }
            }
            await context.SaveChangesAsync();
        }
    }
}
