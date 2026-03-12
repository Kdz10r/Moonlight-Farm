using Microsoft.AspNetCore.SignalR;
using MoonlightFarm.Server.Models;
using MoonlightFarm.Server.Services;

namespace MoonlightFarm.Server.Hubs
{
    public class GameHub : Hub
    {
        private readonly SessionManager _sessionManager;
        private readonly RoomManager _roomManager;

        public GameHub(SessionManager sessionManager, RoomManager roomManager)
        {
            _sessionManager = sessionManager;
            _roomManager = roomManager;
        }

        public async Task JoinRoom(string roomId, string sessionId)
        {
            var room = _roomManager.GetOrCreateRoom(roomId);
            room.Players.TryAdd(Context.ConnectionId, sessionId);
            
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("PlayerJoined", Context.ConnectionId, sessionId);
            
            // Send current room state to the new player
            await Clients.Caller.SendAsync("SyncRoomState", room.Context.State);
        }

        public async Task UpdatePosition(string roomId, float x, float y, int direction)
        {
            await Clients.OthersInGroup(roomId).SendAsync("PlayerMoved", Context.ConnectionId, x, y, direction);
        }

        public async Task ActionPerformed(string roomId, string actionType, int x, int y)
        {
            // Update room state logic would go here if we had server-side authority
            // For now, we just broadcast the action to others
            await Clients.Group(roomId).SendAsync("BroadcastAction", Context.ConnectionId, actionType, x, y);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // We need to find which room the player was in. 
            // In a real app, we might store this in a dictionary or use Context.Items
            // For simplicity, let's assume the client notifies us or we search.
            await base.OnDisconnectedAsync(exception);
        }
    }
}
