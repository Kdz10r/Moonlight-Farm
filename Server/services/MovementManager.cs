using MoonlightFarm.Server.Models;

namespace MoonlightFarm.Server.Services
{
    public static class MovementManager
    {
        public static bool MovePlayer(GameContext context, Direction direction)
        {
            var player = context.State.Player;
            var map = context.State.FarmMap;
            if (context.State.CurrentMineMap != null) map = context.State.CurrentMineMap;
            
            player.Facing = direction; // Zawsze obracamy gracza, nawet jak nie może przejść

            int targetX = player.Position.X;
            int targetY = player.Position.Y;

            switch (direction)
            {
                case Direction.Up: targetY--; break;
                case Direction.Down: targetY++; break;
                case Direction.Left: targetX--; break;
                case Direction.Right: targetX++; break;
            }

            // Walidacja granic i kolizji
            var targetTile = map.GetTile(targetX, targetY);
            
            if (targetTile != null && targetTile.IsWalkable)
            {
                // Sprawdź wejście do jaskini
                if (targetTile.Quality == 99 && context.State.CurrentMineMap == null)
                { 
                    context.State.CurrentMineMap = MiningManager.GenerateMineLevel(1, 20, 20, context.State.Time.IsFullMoon);
                    context.State.CurrentMineLevel = 1;
                    player.Position = new Position(10, 10);
                    context.MarkDirty();
                    return true;
                }

                player.Position.X = targetX;
                player.Position.Y = targetY;
                
                // Ruch zajmuje czas - np. 1 minuta
                TimeManager.AdvanceTime(context, 1);
                
                context.MarkDirty();
                return true;
            }

            return false;
        }
    }
}
