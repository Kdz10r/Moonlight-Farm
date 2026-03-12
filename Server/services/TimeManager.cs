using MoonlightFarm.Server.Models;

namespace MoonlightFarm.Server.Services
{
    public static class TimeManager
    {
        // Metoda do przesuwania czasu o zadaną liczbę minut (np. przy akcji)
        public static void AdvanceTime(GameContext context, int minutes)
        {
            if (minutes <= 0) return;

            context.State.Time.TotalMinutes += minutes;
            context.MarkDirty();
            
            // Tutaj w przyszłości: sprawdzenie triggerów czasowych (np. zmęczenie, noc)
            // Leniwa ewaluacja - nic nie robimy dopóki nie wywołamy tej metody
        }

        public static void Sleep(GameContext context)
        {
            var time = context.State.Time;
            var minutesToday = time.TotalMinutes % GameTime.MinutesInDay;
            var minutesUntilMidnight = GameTime.MinutesInDay - minutesToday;
            var minutesToSleep = minutesUntilMidnight + (6 * 60);
            
            time.TotalMinutes += minutesToSleep;
            
            // Dzienny cykl wzrostu i nawadniania
            ProcessOvernightGrowth(context);
            
            context.MarkDirty();
        }

        private static void ProcessShippingBin(GameState state)
        {
            int totalEarnings = 0;
            foreach (var item in state.ShippingBin)
            {
                int price = GetItemPrice(item);
                totalEarnings += price * item.Count;
            }
            state.Player.Money += totalEarnings;
            state.ShippingBin.Clear();
        }

        private static int GetItemPrice(Item item)
        {
            if (item.Type == "Fish") return 150;
            if (item.Type == "Resource")
            {
                if (item.Name == "Stone") return 2;
                if (item.Name == "Wood") return 2;
                if (item.Name == "Iron Ore") return 50;
                if (item.Name == "Gold Ore") return 150;
            }
            if (item.Type == "Crop") return 80;
            return 10;
        }

        private static void ProcessOvernightGrowth(GameContext context)
        {
            var state = context.State;
            var map = state.FarmMap;
            
            // Losowanie pogody na następny dzień
            var rand = new Random();
            state.Weather = WeatherType.Sunny;
            if (state.Time.IsFullMoon) state.Weather = WeatherType.FullMoonMagic;
            else if (rand.NextDouble() < 0.15) state.Weather = WeatherType.Rain;
            else if (rand.NextDouble() < 0.05) state.Weather = WeatherType.Storm;
            else if (rand.NextDouble() < 0.1) state.Weather = WeatherType.Foggy;

            // Reset energii gracza
            state.Player.Energy = state.Player.MaxEnergy;

            // Proces sprzedaży przedmiotów ze skrzynki wysyłkowej
            ProcessShippingBin(state);

            foreach (var row in map.Tiles)
            {
                foreach (var tile in row)
                {
                    // 1. Podlewanie przez deszcz
                    if (state.Weather == WeatherType.Rain || state.Weather == WeatherType.Storm)
                    {
                        if (tile.Type == TileType.TilledDirt) tile.Moisture = 1.0f;
                    }

                    // 2. Wzrost roślin
                    if (tile.Object?.Type == ObjectType.Crop && tile.Moisture > 0)
                    {
                        bool canGrow = true;
                        if (tile.Object.SubType == "Moon Blossom" && !state.Time.IsNight) canGrow = false;
                        
                        if (canGrow)
                        {
                            int growthBoost = 1;
                            if (tile.Object.SubType == "Silver Wheat" && state.Time.IsFullMoon) growthBoost = 2;
                            
                            tile.Object.GrowthStage += growthBoost;
                            if (tile.Object.GrowthStage > tile.Object.MaxGrowthStage)
                                tile.Object.GrowthStage = tile.Object.MaxGrowthStage;
                        }
                    }
                    tile.Moisture = 0; // Wysychanie ziemi co noc

                    // 3. Rozprzestrzenianie chwastów / kamieni
                    if (tile.Type == TileType.Grass && tile.Object == null)
                    {
                        if (rand.NextDouble() < 0.01)
                        {
                            tile.Object = new WorldObject { Type = ObjectType.Rock, SubType = "Stone", Health = 1 };
                        }
                    }
                }
            }
        }
    }
}
