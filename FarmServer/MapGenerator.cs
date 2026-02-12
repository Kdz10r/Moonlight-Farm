namespace FarmServer
{
    public static class MapGenerator
    {
        public static TileMap GenerateBasicFarm(int width, int height)
        {
            var map = new TileMap(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Domyślnie trawa
                    map.Tiles[y][x] = new Tile(TileType.Grass, true);

                    // Granice mapy jako ściany/woda
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    {
                        map.Tiles[y][x] = new Tile(TileType.Water, false);
                    }
                    
                    // Mały staw na środku
                    if (x >= width / 2 - 2 && x <= width / 2 + 2 &&
                        y >= height / 2 - 2 && y <= height / 2 + 2)
                    {
                         map.Tiles[y][x] = new Tile(TileType.Water, false);
                    }

                    // Trochę kamieni losowo
                    if ((x + y) % 7 == 0 && map.Tiles[y][x].IsWalkable)
                    {
                        map.Tiles[y][x] = new Tile(TileType.Stone, false);
                    }
                }
            }
            
            // Pole uprawne
            for(int y = 5; y < 10; y++)
            {
                for(int x = 5; x < 15; x++)
                {
                     map.Tiles[y][x] = new Tile(TileType.Dirt, true);
                }
            }

            return map;
        }
    }
}
