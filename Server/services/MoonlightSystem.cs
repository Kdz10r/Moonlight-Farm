using MoonlightFarm.Server.Models;
using System;
using System.Linq;

namespace MoonlightFarm.Server.Services
{
    public static class MoonlightSystem
    {
        private static readonly Random _rand = new Random();

        public static void Update(GameContext context)
        {
            var state = context.State;
            var time = state.Time;

            // Moonlight Grove logic (North area: X 40-60, Y 0-20)
            if (time.IsNight)
            {
                SpawnMoonlightObjects(state);
            }
        }

        private static void SpawnMoonlightObjects(GameState state)
        {
            // Only spawn if it's night and with a low chance per tick
            if (_rand.NextDouble() > 0.01) return;

            int x = _rand.Next(40, 60);
            int y = _rand.Next(0, 20);
            
            var tile = state.FarmMap.GetTile(x, y);
            if (tile != null && tile.IsWalkable && tile.Object == null)
            {
                // Full moon increases chance and quality
                double roll = _rand.NextDouble();
                if (state.Time.IsFullMoon || roll < 0.1)
                {
                    tile.Object = new WorldObject
                    {
                        Type = ObjectType.Resource,
                        SubType = "MoonCrystal",
                        Health = 1,
                        MaxHealth = 1
                    };
                }
                else if (roll < 0.3)
                {
                    tile.Object = new WorldObject
                    {
                        Type = ObjectType.Crop,
                        SubType = "Moon Blossom",
                        GrowthStage = 5, // Already grown
                        MaxGrowthStage = 5,
                        Health = 1,
                        MaxHealth = 1
                    };
                }
            }
        }

        public static void ProcessFullMoonEvent(GameContext context)
        {
            var state = context.State;
            if (!state.Time.IsFullMoon) return;

            // During full moon, all crops have a chance to get a quality boost
            // and monsters in the mine are more likely to drop rare items
            Console.WriteLine("A magical full moon event has occurred!");
        }
    }
}
