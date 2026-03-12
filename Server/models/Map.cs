namespace MoonlightFarm.Server.Models
{
    public enum TileType
    {
        Grass = 0,
        Dirt = 1,
        Water = 2,
        Stone = 3,
        WoodFloor = 4,
        Wall = 5,
        TilledDirt = 6,
        Mountain = 7,
        DeepWater = 8
    }

    public class Tile
    {
        public TileType Type { get; set; }
        public bool IsWalkable { get; set; }
        public float Moisture { get; set; } = 0f; // 0.0 - 1.0
        public int Quality { get; set; } = 0; // Jakość gleby
        public WorldObject? Object { get; set; } // Obiekt na kafelku (drzewo, kamień, roślina)

        public Tile() { }

        public Tile(TileType type, bool isWalkable)
        {
            Type = type;
            IsWalkable = isWalkable;
        }
    }

    public enum ObjectType
    {
        Tree,
        Rock,
        Crop,
        Resource,
        Sprinkler,
        ShippingBin
    }

    public class WorldObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ObjectType Type { get; set; }
        public string SubType { get; set; } = ""; // np. "Oak", "IronRock", "Parsnip"
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int GrowthStage { get; set; } = 0;
        public int MaxGrowthStage { get; set; } = 0;
        public long LastUpdateTicks { get; set; }
        public bool IsDiseased { get; set; } = false;
    }

    public class TileMap
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public Tile[][] Tiles { get; set; }

        public TileMap(int width, int height)
        {
            Width = width;
            Height = height;
            Tiles = new Tile[height][];
            for (int y = 0; y < height; y++)
            {
                Tiles[y] = new Tile[width];
            }
        }

        public Tile? GetTile(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return null; // Poza mapą
            return Tiles[y][x];
        }
    }
}
