using MoonlightFarm.Server.Services;

namespace MoonlightFarm.Server.Models
{
    public enum WeatherType
    {
        Sunny,
        Rain,
        Storm,
        Foggy,
        FullMoonMagic
    }

    // Główny kontener stanu gry, który będzie serializowany
    public class GameState
    {
        public string FarmName { get; set; } = "Moonlight Farm";
        public long CreatedAt { get; set; }
        public long LastSavedAt { get; set; }
        
        public GameTime Time { get; set; } = new GameTime();
        public WeatherType Weather { get; set; } = WeatherType.Sunny;
        
        public TileMap FarmMap { get; set; }
        public List<AnimalData> Animals { get; set; } = new List<AnimalData>();
        public List<NPCData> NPCs { get; set; } = new List<NPCData>();
        public List<Item> ShippingBin { get; set; } = new List<Item>();
        public int CurrentMineLevel { get; set; } = 0;
        public TileMap? CurrentMineMap { get; set; } = null;
        public List<MonsterData> Monsters { get; set; } = new List<MonsterData>();
        public PlayerData Player { get; set; } = new PlayerData();

        public GameState()
        {
            CreatedAt = DateTime.UtcNow.Ticks;
            FarmMap = MapGenerator.GenerateAdvancedFarm(100, 100);
            InitializePlayer();
            InitializeNPCs();
            InitializeAnimals();
        }

        private void InitializeAnimals()
        {
            Animals.Add(new AnimalData { Type = "Chicken", Position = new Position(45, 45) });
            Animals.Add(new AnimalData { Type = "Cow", Position = new Position(40, 40) });
            Animals.Add(new AnimalData { Type = "MoonFox", Position = new Position(50, 5) });
        }

        private void InitializeNPCs()
        {
            NPCs.Add(new NPCData { Name = "Blacksmith", Type = "Smith", Position = new Position(85, 85) });
            NPCs.Add(new NPCData { Name = "Innkeeper", Type = "Merchant", Position = new Position(80, 80) });
            NPCs.Add(new NPCData { Name = "Moon Priestess", Type = "Magic", Position = new Position(50, 10) });
        }

        private void InitializePlayer()
        {
            Player.Inventory.Add(new Item { Name = "Old Pickaxe", Type = "Tool", Durability = 100 });
            Player.Inventory.Add(new Item { Name = "Rusty Axe", Type = "Tool", Durability = 100 });
            Player.Inventory.Add(new Item { Name = "Basic Hoe", Type = "Tool", Durability = 100 });
            Player.Inventory.Add(new Item { Name = "Watering Can", Type = "Tool", Durability = 40 });
            Player.Inventory.Add(new Item { Name = "Bamboo Rod", Type = "Tool", Durability = 100 });
            Player.Inventory.Add(new Item { Name = "Parsnip Seeds", Type = "Seed", Count = 15 });
            Player.Inventory.Add(new Item { Name = "Moon Blossom Seeds", Type = "Seed", Count = 5 });
            Player.Inventory.Add(new Item { Name = "Silver Wheat Seeds", Type = "Seed", Count = 5 });
        }
    }

    public class MonsterData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = "Slime"; // Slime, Bat, Golem
        public Position Position { get; set; } = new Position();
        public int Health { get; set; } = 10;
        public int MaxHealth { get; set; } = 10;
    }

    public class AnimalData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = "Chicken"; // Chicken, Cow, Sheep, MoonFox
        public Position Position { get; set; } = new Position();
        public int Friendship { get; set; } = 0;
        public bool IsFed { get; set; } = false;
        public bool WasPetted { get; set; } = false;
        public int Age { get; set; } = 0;
    }

    public class NPCData
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public Position Position { get; set; } = new Position();
        public int Friendship { get; set; } = 0; // 0-1000 (10 hearts)
        public int LastTalkedDay { get; set; } = -1;
        public Dictionary<string, int> GiftHistory { get; set; } = new Dictionary<string, int>();
    }

    public class PlayerData
    {
        public int Money { get; set; } = 500;
        public int Level { get; set; } = 1;
        public int Energy { get; set; } = 100;
        public int MaxEnergy { get; set; } = 100;
        public Position Position { get; set; } = new Position(50, 50);
        public Direction Facing { get; set; } = Direction.Down;
        public List<Item> Inventory { get; set; } = new List<Item>();
        public int InventorySlots { get; set; } = 12; // Startowy limit slotów
        public int SelectedSlot { get; set; } = 0;
        public Dictionary<string, int> Skills { get; set; } = new Dictionary<string, int>
        {
            { "Farming", 0 },
            { "Mining", 0 },
            { "Foraging", 0 },
            { "Fishing", 0 }
        };
    }

    public class Item
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = ""; // Tool, Seed, Resource
        public int Count { get; set; } = 1;
        public int Durability { get; set; } = 100;
        public int MaxDurability { get; set; } = 100;
    }
}
