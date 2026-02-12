namespace FarmServer
{
    // Główny kontener stanu gry, który będzie serializowany
    public class GameState
    {
        public string FarmName { get; set; } = "Moonlight Farm";
        public long CreatedAt { get; set; }
        public long LastSavedAt { get; set; }
        
        public GameTime Time { get; set; } = new GameTime();
        
        public TileMap FarmMap { get; set; }
        public PlayerData Player { get; set; } = new PlayerData();

        public GameState()
        {
            CreatedAt = DateTime.UtcNow.Ticks;
            // Generuj mapę przy tworzeniu nowej gry
            FarmMap = MapGenerator.GenerateBasicFarm(40, 30);
        }
    }

    public class PlayerData
    {
        public int Money { get; set; } = 0;
        public int Level { get; set; } = 1;
        public Position Position { get; set; } = new Position(10, 10); // Startowa pozycja
        public Direction Facing { get; set; } = Direction.Down;
    }
}
