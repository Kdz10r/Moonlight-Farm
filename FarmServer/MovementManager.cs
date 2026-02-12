namespace FarmServer
{
    public static class MovementManager
    {
        public static bool MovePlayer(GameContext context, Direction direction)
        {
            var player = context.State.Player;
            var map = context.State.FarmMap;
            
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
