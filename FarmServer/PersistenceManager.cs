using System.Text.Json;

namespace FarmServer
{
    public class PersistenceManager
    {
        private readonly string _saveDirectory;

        public PersistenceManager(IConfiguration configuration)
        {
            // Pobierz ścieżkę z konfiguracji lub użyj domyślnej
            var configuredPath = configuration["SaveDirectory"];
            _saveDirectory = string.IsNullOrWhiteSpace(configuredPath) 
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves") 
                : configuredPath;

            if (!Directory.Exists(_saveDirectory))
            {
                Directory.CreateDirectory(_saveDirectory);
            }
        }

        public async Task SaveGameAsync(string sessionId, GameState state)
        {
            state.LastSavedAt = DateTime.UtcNow.Ticks;
            var filePath = GetSavePath(sessionId);
            
            // Asynchroniczny zapis z użyciem FileStream
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await JsonSerializer.SerializeAsync(stream, state, new JsonSerializerOptions { WriteIndented = false });
        }

        public async Task<GameState?> LoadGameAsync(string sessionId)
        {
            var filePath = GetSavePath(sessionId);
            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                return await JsonSerializer.DeserializeAsync<GameState>(stream);
            }
            catch (Exception)
            {
                // W przypadku błędu odczytu (np. uszkodzony plik) zwróć null lub rzuć wyjątek
                // Na razie zakładamy bezpieczny powrót null -> nowa gra
                return null;
            }
        }

        public bool SaveExists(string sessionId)
        {
            return File.Exists(GetSavePath(sessionId));
        }

        private string GetSavePath(string sessionId)
        {
            // Sanityzacja ID sesji dla bezpieczeństwa ścieżki
            var safeId = string.Join("_", sessionId.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_saveDirectory, $"{safeId}.json");
        }
    }
}
