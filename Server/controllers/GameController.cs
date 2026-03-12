using Microsoft.AspNetCore.Mvc;
using MoonlightFarm.Server.Models;
using MoonlightFarm.Server.Services;
using MoonlightFarm.Server.Services;

namespace MoonlightFarm.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly SessionManager _sessionManager;
        private readonly RoomManager _roomManager;

        public GameController(SessionManager sessionManager, RoomManager roomManager)
        {
            _sessionManager = sessionManager;
            _roomManager = roomManager;
        }

        [HttpGet("state")]
        public IActionResult GetState(string? roomId)
        {
            var session = HttpContext.Items["Session"] as Session;
            GameContext? gameContext = session?.Context;

            if (!string.IsNullOrEmpty(roomId))
            {
                gameContext = _roomManager.GetOrCreateRoom(roomId).Context;
            }

            return Ok(new
            {
                Status = "Active",
                SessionId = session?.Id,
                GameState = gameContext?.State,
                DisplayTime = gameContext?.State.Time.DisplayTime
            });
        }

        [HttpPost("sleep")]
        public IActionResult Sleep(string? roomId)
        {
            var session = HttpContext.Items["Session"] as Session;
            if (session == null) return Unauthorized();

            GameContext gameContext = session.Context;
            if (!string.IsNullOrEmpty(roomId) && _roomManager.TryGetRoom(roomId, out var room) && room != null)
            {
                gameContext = room.Context;
            }

            TimeManager.Sleep(gameContext);
            
            return Ok(new
            {
                Message = "You slept until 6:00 AM the next day.",
                DisplayTime = gameContext.State.Time.DisplayTime
            });
        }

        [HttpPost("move")]
        public IActionResult Move([FromQuery] Direction direction, string? roomId)
        {
            var session = HttpContext.Items["Session"] as Session;
            if (session == null) return Unauthorized();

            bool moved = MovementManager.MovePlayer(session.Context, direction);

            return Ok(new
            {
                Success = moved,
                Position = session.Context.State.Player.Position,
                Facing = session.Context.State.Player.Facing.ToString()
            });
        }

        [HttpPost("use-tool")]
        public IActionResult UseTool(int x, int y, string? roomId)
        {
            var session = HttpContext.Items["Session"] as Session;
            if (session == null) return Unauthorized();

            GameContext gameContext = session.Context;
            if (!string.IsNullOrEmpty(roomId) && _roomManager.TryGetRoom(roomId, out var room) && room != null)
            {
                gameContext = room.Context;
            }

            bool success = ToolManager.UseTool(gameContext, x, y);

            return Ok(new
            {
                Success = success,
                GameState = gameContext.State
            });
        }

        [HttpPost("plant")]
        public IActionResult Plant(int x, int y, string seedName, string? roomId)
        {
            var session = HttpContext.Items["Session"] as Session;
            if (session == null) return Unauthorized();

            GameContext gameContext = session.Context;
            if (!string.IsNullOrEmpty(roomId) && _roomManager.TryGetRoom(roomId, out var room) && room != null)
            {
                gameContext = room.Context;
            }

            bool success = FarmingManager.PlantSeed(gameContext, x, y, seedName);

            return Ok(new { Success = success, GameState = gameContext.State });
        }

        [HttpPost("action/upgrade-tool")]
        public IActionResult UpgradeTool(string toolName, string? roomId)
        {
            var session = HttpContext.Items["Session"] as Session;
            GameContext? gameContext = session?.Context;

            if (!string.IsNullOrEmpty(roomId))
            {
                gameContext = _roomManager.GetOrCreateRoom(roomId).Context;
            }

            if (gameContext == null) return NotFound();

            bool success = ToolManager.UpgradeTool(gameContext.State.Player, toolName);
            return Ok(new { Success = success });
        }

        [HttpPost("action/fish")]
        public IActionResult Fish(string? roomId)
        {
            var session = HttpContext.Items["Session"] as Session;
            GameContext? gameContext = session?.Context;

            if (!string.IsNullOrEmpty(roomId))
            {
                gameContext = _roomManager.GetOrCreateRoom(roomId).Context;
            }

            if (gameContext == null) return NotFound();

            var player = gameContext.State.Player;
            if (player.Energy < 5) return BadRequest(new { Message = "Too tired to fish!" });

            var fish = FishingManager.CatchFish(gameContext);
            bool added = FishingManager.TryAddFishToInventory(player, fish);

            if (added)
            {
                player.Energy -= 5;
                player.Skills["Fishing"] += 10;
                return Ok(new { Success = true, Fish = fish });
            }

            return BadRequest(new { Message = "Inventory full!" });
        }

        [HttpPost("action/ship-item")]
        public IActionResult ShipItem(string? roomId)
        {
            var session = HttpContext.Items["Session"] as Session;
            GameContext? gameContext = session?.Context;
            if (!string.IsNullOrEmpty(roomId)) gameContext = _roomManager.GetOrCreateRoom(roomId).Context;
            if (gameContext == null) return NotFound();

            var player = gameContext.State.Player;
            if (player.Inventory.Count <= player.SelectedSlot) return BadRequest();

            var item = player.Inventory[player.SelectedSlot];
            if (item == null || item.Type == "Tool") return BadRequest(new { Message = "Cannot ship tools!" });

            gameContext.State.ShippingBin.Add(item);
            player.Inventory.RemoveAt(player.SelectedSlot);
            
            gameContext.MarkDirty();
            return Ok(new { Success = true });
        }

        [HttpPost("action/talk")]
        public IActionResult Talk(string npcName, string? roomId)
        {
            var session = HttpContext.Items["Session"] as Session;
            GameContext? gameContext = session?.Context;

            if (!string.IsNullOrEmpty(roomId))
            {
                gameContext = _roomManager.GetOrCreateRoom(roomId).Context;
            }

            if (gameContext == null) return NotFound();

            var npc = gameContext.State.NPCs.FirstOrDefault(n => n.Name == npcName);
            if (npc == null) return NotFound();

            string dialogue = NPCManager.GetDialogue(npc, gameContext.State);
            NPCManager.TalkToNPC(gameContext.State.Player, npc, gameContext.State.Time.CurrentDay);

            return Ok(new { Dialogue = dialogue });
        }

        [HttpPost("action/give-gift")]
        public IActionResult GiveGift(string npcName, string? roomId)
        {
            var session = HttpContext.Items["Session"] as Session;
            GameContext? gameContext = session?.Context;

            if (!string.IsNullOrEmpty(roomId))
            {
                gameContext = _roomManager.GetOrCreateRoom(roomId).Context;
            }

            if (gameContext == null) return NotFound();

            var npc = gameContext.State.NPCs.FirstOrDefault(n => n.Name == npcName);
            if (npc == null) return NotFound();

            var player = gameContext.State.Player;
            var item = player.Inventory[player.SelectedSlot];

            var result = NPCManager.GiveGift(player, npc, item);
            return Ok(new { Success = result.Success, Message = result.Message });
        }

        [HttpPost("action/propose")]
        public IActionResult Propose(string npcName, string? roomId)
        {
            var session = HttpContext.Items["Session"] as Session;
            GameContext? gameContext = session?.Context;

            if (!string.IsNullOrEmpty(roomId))
            {
                gameContext = _roomManager.GetOrCreateRoom(roomId).Context;
            }

            if (gameContext == null) return NotFound();

            var npc = gameContext.State.NPCs.FirstOrDefault(n => n.Name == npcName);
            if (npc == null) return NotFound();

            if (npc.Friendship >= 800)
            {
                return Ok(new { Success = true, Message = $"Och! Tak, wyjdę za ciebie! Od teraz będziemy zarządzać {gameContext.State.FarmName} razem." });
            }
            else
            {
                return Ok(new { Success = false, Message = "To trochę za wcześnie na takie deklaracje... Musimy się lepiej poznać." });
            }
        }
    }
}
