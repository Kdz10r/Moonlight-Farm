using System.Collections.Concurrent;

namespace MoonlightFarm.Server.Models
{
    public class Room
    {
        public string Id { get; set; }
        public GameContext Context { get; set; }
        public ConcurrentDictionary<string, string> Players { get; set; } = new(); // ConnectionId -> SessionId

        public Room(string id)
        {
            Id = id;
            Context = new GameContext(new GameState());
        }
    }
}
