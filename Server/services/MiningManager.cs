using MoonlightFarm.Server.Models;

namespace MoonlightFarm.Server.Services
{
    public static class MiningManager
    {
        private static Random _rand = new Random();

        public static TileMap GenerateMineLevel(int level, int width, int height, bool isFullMoon = false)
        {
            var map = new TileMap(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Basic cave floor
                    map.Tiles[y][x] = new Tile(TileType.Stone, true);
                    
                    // Walls on borders
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    {
                        map.Tiles[y][x] = new Tile(TileType.Mountain, false);
                        continue;
                    }

                    // Random obstacles
                    if (_rand.NextDouble() < 0.2)
                    {
                        map.Tiles[y][x] = new Tile(TileType.Mountain, false);
                    }
                    else if (_rand.NextDouble() < 0.1)
                    {
                        // Ores and rocks
                        double goldChance = isFullMoon ? 0.3 : 0.1;
                        var subType = _rand.NextDouble() > (1.0 - goldChance) ? "Gold" : (_rand.NextDouble() > 0.7 ? "Iron" : "Stone");
                        map.Tiles[y][x].Object = new WorldObject { 
                            Type = ObjectType.Rock, 
                            SubType = subType, 
                            Health = 5, 
                            MaxHealth = 5 
                        };
                    }
                }
            }
            return map;
        }

        public static void UpdateMonsters(GameContext context)
        {
            var state = context.State;
            if (state.CurrentMineMap == null) return;

            foreach (var monster in state.Monsters)
            {
                // Simple AI: move towards player if close
                int dx = Math.Sign(state.Player.Position.X - monster.Position.X);
                int dy = Math.Sign(state.Player.Position.Y - monster.Position.Y);

                if (_rand.NextDouble() < 0.3)
                {
                    int newX = monster.Position.X + dx;
                    int newY = monster.Position.Y + dy;

                    if (newX >= 0 && newX < state.CurrentMineMap.Width && newY >= 0 && newY < state.CurrentMineMap.Height)
                    {
                        if (state.CurrentMineMap.Tiles[newY][newX].IsWalkable)
                        {
                            monster.Position.X = newX;
                            monster.Position.Y = newY;
                        }
                    }
                }
            }
        }
    }
}
