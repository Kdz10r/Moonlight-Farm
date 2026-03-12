using MoonlightFarm.Server.Models;

namespace MoonlightFarm.Server.Services
{
    public static class AnimalManager
    {
        private static Random _rand = new Random();

        public static void UpdateAnimals(GameContext context)
        {
            var state = context.State;
            bool isNight = state.Time.IsNight;

            foreach (var animal in state.Animals)
            {
                // MoonFox is only active at night
                if (animal.Type == "MoonFox" && !isNight) continue;

                // Random movement
                if (_rand.NextDouble() < 0.2)
                {
                    int dx = _rand.Next(-1, 2);
                    int dy = _rand.Next(-1, 2);
                    int newX = Math.Clamp(animal.Position.X + dx, 0, state.FarmMap.Width - 1);
                    int newY = Math.Clamp(animal.Position.Y + dy, 0, state.FarmMap.Height - 1);
                    
                    if (state.FarmMap.Tiles[newY][newX].IsWalkable)
                    {
                        animal.Position.X = newX;
                        animal.Position.Y = newY;
                    }
                }
            }
        }

        public static void ProcessOvernight(GameContext context)
        {
            var state = context.State;
            foreach (var animal in state.Animals)
            {
                if (animal.IsFed)
                {
                    animal.Friendship = Math.Min(animal.Friendship + 10, 1000);
                    // Produce items based on type
                    ProduceItem(context, animal);
                }
                else
                {
                    animal.Friendship = Math.Max(animal.Friendship - 20, 0);
                }

                animal.IsFed = false;
                animal.WasPetted = false;
                animal.Age++;
            }
        }

        private static void ProduceItem(GameContext context, AnimalData animal)
        {
            string itemName = animal.Type switch
            {
                "Chicken" => "Egg",
                "Cow" => "Milk",
                "Sheep" => "Wool",
                "MoonFox" => "Moon Essence",
                _ => "Unknown Product"
            };

            // In a real game, we'd drop this on the floor or put in a box
            // For now, let's just add a message or log
        }
    }
}
