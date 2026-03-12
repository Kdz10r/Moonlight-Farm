using System.Collections.Concurrent;

using MoonlightFarm.Server.Models;
using MoonlightFarm.Server.Services;

namespace MoonlightFarm.Server.Services
{
    public class RoomManager
    {
        private readonly ConcurrentDictionary<string, Room> _rooms = new();
        private readonly ILogger<RoomManager> _logger;

        public RoomManager(ILogger<RoomManager> logger)
        {
            _logger = logger;
        }

        public Room GetOrCreateRoom(string roomId)
        {
            return _rooms.GetOrAdd(roomId, id => 
            {
                _logger.LogInformation($"Created new room: {id}");
                return new Room(id);
            });
        }

        public bool TryGetRoom(string roomId, out Room? room)
        {
            return _rooms.TryGetValue(roomId, out room);
        }

        public IEnumerable<Room> GetAllRooms()
        {
            return _rooms.Values;
        }

        public void RemovePlayerFromRoom(string roomId, string connectionId)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                room.Players.TryRemove(connectionId, out _);
                if (room.Players.IsEmpty)
                {
                    // Optionally remove empty rooms
                    // _rooms.TryRemove(roomId, out _);
                }
            }
        }
    }
}
