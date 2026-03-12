using MoonlightFarm.Server.Models;

namespace MoonlightFarm.Server.Services
{
    public static class FarmingManager
    {
        public static bool PlantSeed(GameContext context, int x, int y, string seedName)
        {
            var tile = context.State.FarmMap.GetTile(x, y);
            if (tile == null || tile.Type != TileType.TilledDirt || tile.Object != null) return false;

            var player = context.State.Player;
            var seedItem = player.Inventory.FirstOrDefault(i => i.Name == seedName && i.Count > 0);
            if (seedItem == null) return false;

            tile.Object = new WorldObject
            {
                Type = ObjectType.Crop,
                SubType = seedName.Replace(" Seeds", ""),
                Health = 1,
                MaxHealth = 1,
                GrowthStage = 0,
                MaxGrowthStage = 5,
                LastUpdateTicks = DateTime.UtcNow.Ticks
            };

            seedItem.Count--;
            if (seedItem.Count <= 0) player.Inventory.Remove(seedItem);

            context.MarkDirty();
            return true;
        }

        public static void UpdateCrops(GameContext context)
        {
            // Logika chorób i nawadniania automatycznego (zraszacze)
            var map = context.State.FarmMap;
            foreach (var row in map.Tiles)
            {
                foreach (var tile in row)
                {
                    if (tile.Object?.Type == ObjectType.Sprinkler)
                    {
                        WaterAround(map, tile);
                    }

                    if (tile.Object?.Type == ObjectType.Crop)
                    {
                        // Losowa choroba
                        if (new Random().NextDouble() < 0.005) tile.Object.IsDiseased = true;
                    }
                }
            }
        }

        private static void WaterAround(TileMap map, Tile sprinklerTile)
        {
            // Prosty zraszacz 3x3
            // Znajdź x, y zraszacza (wymagałoby to zmiany w modelu danych lub przeszukania)
            // Na razie pominiemy dla uproszczenia wydajności
        }
    }
}
