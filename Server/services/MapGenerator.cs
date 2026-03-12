using MoonlightFarm.Server.Models;

namespace MoonlightFarm.Server.Services
{
    public static class MapGenerator
    {
        private static Random _rand = new Random();

        public static TileMap GenerateAdvancedFarm(int width, int height)
        {
            var map = new TileMap(width, height);
            
            // 1. Podstawowy teren i biomy
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Center of the map is the farm
                    float distToCenter = (float)Math.Sqrt(Math.Pow(x - width/2, 2) + Math.Pow(y - height/2, 2));
                    
                    // 🏡 Farm Area (Center)
                    if (distToCenter < 20) {
                        map.Tiles[y][x] = new Tile(TileType.Grass, true);
                        if (x > width/2 - 5 && x < width/2 + 5 && y > height/2 - 5 && y < height/2 + 5) {
                            if ((x + y) % 4 == 0) map.Tiles[y][x].Type = TileType.Dirt;
                        }
                        continue;
                    }

                    // 📦 Shipping Bin (near center)
                    if (x == width / 2 + 2 && y == height / 2)
                    {
                        map.Tiles[y][x] = new Tile(TileType.WoodFloor, true);
                        map.Tiles[y][x].Object = new WorldObject { Type = ObjectType.ShippingBin, Health = 1, MaxHealth = 1 };
                        continue;
                    }

                    // 🏘️ Town Area (South East)
                    if (x > width * 0.7 && y > height * 0.6) {
                        map.Tiles[y][x] = new Tile(TileType.WoodFloor, true);
                        if (x % 10 == 0 || y % 10 == 0) map.Tiles[y][x].Type = TileType.Stone; // Roads
                        continue;
                    }

                    // 🌲 Forest Area (North West)
                    if (x < width * 0.3 && y < height * 0.4) {
                        map.Tiles[y][x] = new Tile(TileType.Grass, true);
                        if (_rand.NextDouble() < 0.3) {
                            map.Tiles[y][x].Object = new WorldObject { Type = ObjectType.Tree, SubType = "Pine", Health = 10, MaxHealth = 10 };
                        }
                        continue;
                    }

                    // 🌊 Lake Area (South West)
                    if (x < width * 0.3 && y > height * 0.6) {
                        map.Tiles[y][x] = new Tile(TileType.Water, false);
                        if (x < width * 0.2 && y > height * 0.7) map.Tiles[y][x].Type = TileType.DeepWater;
                        continue;
                    }

                    // ⛰️ Cave / Mountain (North East)
                    if (x > width * 0.7 && y < height * 0.4) {
                        map.Tiles[y][x] = new Tile(TileType.Mountain, false);
                        // Cave entrance
                        if (x == (int)(width * 0.85) && y == (int)(height * 0.2)) {
                            map.Tiles[y][x] = new Tile(TileType.Stone, true);
                            map.Tiles[y][x].Quality = 99; // Special flag for cave entrance
                        }
                        else if (_rand.NextDouble() < 0.1) map.Tiles[y][x] = new Tile(TileType.Stone, true); 
                        continue;
                    }

                    // 🌙 Moonlight Grove (Hidden, North)
                    if (x > width * 0.4 && x < width * 0.6 && y < height * 0.2) {
                        map.Tiles[y][x] = new Tile(TileType.Grass, true);
                        map.Tiles[y][x].Moisture = 0.8f; // Magical glow
                        if (_rand.NextDouble() < 0.05) {
                            map.Tiles[y][x].Object = new WorldObject { 
                                Type = ObjectType.Resource, 
                                SubType = "MoonCrystal", 
                                Health = 1, 
                                MaxHealth = 1 
                            };
                        }
                        continue;
                    }

                    // Filling rest with Grass and some Dirt
                    double noise = Math.Sin(x * 0.1) * Math.Cos(y * 0.1) + _rand.NextDouble() * 0.1;
                    if (noise > 0.4) map.Tiles[y][x] = new Tile(TileType.Dirt, true);
                    else map.Tiles[y][x] = new Tile(TileType.Grass, true);
                }
            }

            // 3. Scattered Objects
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var tile = map.Tiles[y][x];
                    if (tile.Object != null || !tile.IsWalkable) continue;

                    double spawnRoll = _rand.NextDouble();
                    if (spawnRoll < 0.02) { // Random trees
                        tile.Object = new WorldObject { Type = ObjectType.Tree, SubType = "Oak", Health = 10, MaxHealth = 10 };
                    } else if (spawnRoll < 0.04) { // Random rocks
                        tile.Object = new WorldObject { Type = ObjectType.Rock, SubType = _rand.NextDouble() > 0.8 ? "Iron" : "Stone", Health = 5, MaxHealth = 5 };
                    }
                }
            }

            return map;
        }

        private static void GenerateRiver(TileMap map, int x1, int y1, int x2, int y2)
        {
            int currX = x1;
            int currY = y1;
            while (currX < x2)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    var tile = map.GetTile(currX, currY + dy);
                    if (tile != null)
                    {
                        tile.Type = TileType.Water;
                        tile.IsWalkable = false;
                    }
                }
                currX++;
                if (_rand.NextDouble() > 0.7) currY += _rand.Next(3) - 1;
                currY = Math.Clamp(currY, 2, map.Height - 3);
            }
        }
    }
}
