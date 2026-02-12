namespace FarmServer
{
    public enum TileType
    {
        Grass = 0,
        Dirt = 1,
        Water = 2,
        Stone = 3,
        WoodFloor = 4,
        Wall = 5
    }

    public class Tile
    {
        public TileType Type { get; set; }
        public bool IsWalkable { get; set; }

        // Konstruktor domyślny dla serializatora
        public Tile() { }

        public Tile(TileType type, bool isWalkable)
        {
            Type = type;
            IsWalkable = isWalkable;
        }
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

        public Tile GetTile(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return null; // Poza mapą
            return Tiles[y][x];
        }
    }
}
