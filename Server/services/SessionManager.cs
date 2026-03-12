using System.Collections.Concurrent;

using MoonlightFarm.Server.Models;
using MoonlightFarm.Server.Services;

namespace MoonlightFarm.Server.Services
{
    public class SessionManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, Session> _activeSessions = new();
        private readonly PersistenceManager _persistenceManager;
        private readonly ILogger<SessionManager> _logger;
        private readonly Timer _cleanupTimer;
        
        // Konfiguracja limitów
        private const int MaxActiveSessions = 100; // Twardy limit aktywnych sesji w RAM
        private readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(5); // Czas bezczynności do uśpienia
        private readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(1); // Jak często sprawdzać

        public SessionManager(PersistenceManager persistenceManager, ILogger<SessionManager> logger)
        {
            _persistenceManager = persistenceManager;
            _logger = logger;
            _cleanupTimer = new Timer(CleanupRoutine, null, CleanupInterval, CleanupInterval);
        }

        public async Task<Session?> GetSessionAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return null;

            // 1. Sprawdź czy sesja jest w pamięci (HOT)
            if (_activeSessions.TryGetValue(sessionId, out var session))
            {
                session.Touch();
                return session;
            }

            // 2. Jeśli nie ma w pamięci, sprawdź czy istnieje zapis na dysku (COLD -> HOT)
            // Tutaj musimy sprawdzić limit aktywnych sesji przed załadowaniem nowej
            if (_activeSessions.Count >= MaxActiveSessions)
            {
                // Wymuś natychmiastowe zwolnienie miejsca (najstarsze sesje)
                await EvictOldestSessionsAsync(1);
            }
            
            // Ponowne sprawdzenie limitu (na wypadek gdyby evikcja się nie powiodła lub inny wątek zajął)
            if (_activeSessions.Count >= MaxActiveSessions)
            {
                _logger.LogWarning("Server overloaded: Max active sessions reached.");
                return null; // Lub rzuć wyjątek 503 Service Unavailable
            }

            if (_persistenceManager.SaveExists(sessionId))
            {
                var gameState = await _persistenceManager.LoadGameAsync(sessionId);
                if (gameState != null)
                {
                    var newSession = new Session(sessionId, new GameContext(gameState));
                    if (_activeSessions.TryAdd(sessionId, newSession))
                    {
                        _logger.LogInformation($"Session {sessionId} restored from disk.");
                        return newSession;
                    }
                }
            }

            return null;
        }

        public async Task<Session> CreateSessionAsync()
        {
            if (_activeSessions.Count >= MaxActiveSessions)
            {
                await EvictOldestSessionsAsync(1);
            }

            var sessionId = SecurityUtils.GenerateSessionId();
            var gameState = new GameState(); // Nowy stan gry
            var session = new Session(sessionId, new GameContext(gameState));
            
            // Zapisz od razu, aby zarezerwować plik
            await _persistenceManager.SaveGameAsync(sessionId, gameState);
            
            _activeSessions.TryAdd(sessionId, session);
            _logger.LogInformation($"New session created: {sessionId}");
            
            return session;
        }

        private async void CleanupRoutine(object? state)
        {
            try
            {
                await EvictIdleSessionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session cleanup routine");
            }
        }

        private async Task EvictIdleSessionsAsync()
        {
            var now = DateTime.UtcNow;
            var idleSessions = _activeSessions.Values
                .Where(s => now - s.LastActive > SessionTimeout)
                .ToList();

            foreach (var session in idleSessions)
            {
                await UnloadSessionAsync(session);
            }
        }

        private async Task EvictOldestSessionsAsync(int count)
        {
            // Znajdź sesje o najstarszym LastActive
            var oldestSessions = _activeSessions.Values
                .OrderBy(s => s.LastActive)
                .Take(count)
                .ToList();

            foreach (var session in oldestSessions)
            {
                await UnloadSessionAsync(session);
            }
        }

        private async Task UnloadSessionAsync(Session session)
        {
            // Usuń z mapy aktywnych sesji
            if (_activeSessions.TryRemove(session.Id, out _))
            {
                // Zapisz stan przed usunięciem
                await _persistenceManager.SaveGameAsync(session.Id, session.Context.State);
                _logger.LogInformation($"Session {session.Id} unloaded to disk.");
            }
        }
        
        // Metoda do ręcznego zapisu (np. przy zamykaniu serwera)
        public async Task SaveAllActiveSessionsAsync()
        {
             foreach (var session in _activeSessions.Values)
             {
                 await _persistenceManager.SaveGameAsync(session.Id, session.Context.State);
             }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }
}
