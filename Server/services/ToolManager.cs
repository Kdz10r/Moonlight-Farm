using MoonlightFarm.Server.Models;

namespace MoonlightFarm.Server.Services
{
    public static class ToolManager
    {
        public static bool UseTool(GameContext context, int x, int y)
        {
            var player = context.State.Player;
            var tile = context.State.FarmMap.GetTile(x, y);
            if (tile == null) return false;

            var selectedItem = player.Inventory.Count > player.SelectedSlot ? player.Inventory[player.SelectedSlot] : null;
            if (selectedItem == null || selectedItem.Type != "Tool") return false;

            bool success = false;
            switch (selectedItem.Name)
            {
                case "Old Pickaxe":
                    success = HandleMining(context, tile);
                    break;
                case "Rusty Axe":
                    success = HandleWoodcutting(context, tile);
                    break;
                case "Basic Hoe":
                    success = HandleTilling(context, tile);
                    break;
                case "Watering Can":
                    success = HandleWatering(context, tile, selectedItem);
                    break;
            }

            if (success)
            {
                selectedItem.Durability--;
                player.Energy -= 2; // Koszt użycia narzędzia
                context.MarkDirty();
            }

            return success;
        }

        public static bool UpgradeTool(PlayerData player, string toolName)
        {
            var tool = player.Inventory.FirstOrDefault(i => i.Name == toolName && i.Type == "Tool");
            if (tool == null) return false;

            int cost = tool.Name.Contains("Old") || tool.Name.Contains("Rusty") || tool.Name.Contains("Basic") ? 500 : 2000;
            if (player.Money < cost) return false;

            player.Money -= cost;
            tool.MaxDurability += 50;
            tool.Durability = tool.MaxDurability;
            
            if (tool.Name.StartsWith("Old ")) tool.Name = tool.Name.Replace("Old ", "Iron ");
            else if (tool.Name.StartsWith("Rusty ")) tool.Name = tool.Name.Replace("Rusty ", "Iron ");
            else if (tool.Name.StartsWith("Basic ")) tool.Name = tool.Name.Replace("Basic ", "Iron ");
            else if (tool.Name.StartsWith("Iron ")) tool.Name = tool.Name.Replace("Iron ", "Gold ");

            return true;
        }

        private static bool HandleMining(GameContext context, Tile tile)
        {
            if (tile.Object?.Type == ObjectType.Rock)
            {
                tile.Object.Health--;
                if (tile.Object.Health <= 0)
                {
                    // Drop resources
                    string resource = tile.Object.SubType == "Iron" ? "Iron Ore" : "Stone";
                    AddItemToInventory(context.State.Player, resource, 1);
                    tile.Object = null;
                    ProgressionManager.AddExperience(context.State.Player, "Mining", 10);
                }
                return true;
            }
            
            if (tile.Object?.SubType == "MoonCrystal")
            {
                AddItemToInventory(context.State.Player, "Moon Crystal", 1);
                tile.Object = null;
                ProgressionManager.AddExperience(context.State.Player, "Mining", 25);
                return true;
            }
            
            return false;
        }

        private static bool HandleWoodcutting(GameContext context, Tile tile)
        {
            if (tile.Object?.Type == ObjectType.Tree)
            {
                tile.Object.Health--;
                if (tile.Object.Health <= 0)
                {
                    string resource = tile.Object.SubType == "Oak" ? "Oak Wood" : "Pine Wood";
                    AddItemToInventory(context.State.Player, resource, 3);
                    tile.Object = null;
                    ProgressionManager.AddExperience(context.State.Player, "Foraging", 10);
                }
                return true;
            }
            return false;
        }

        private static bool HandleTilling(GameContext context, Tile tile)
        {
            if (tile.Object?.Type == ObjectType.Crop && tile.Object.GrowthStage >= tile.Object.MaxGrowthStage)
            {
                // Harvest
                string resource = tile.Object.SubType;
                AddItemToInventory(context.State.Player, resource, 1);
                tile.Object = null;
                ProgressionManager.AddExperience(context.State.Player, "Farming", 10);
                return true;
            }

            if (tile.Type == TileType.Grass || tile.Type == TileType.Dirt)
            {
                tile.Type = TileType.TilledDirt;
                tile.Quality = 10;
                ProgressionManager.AddExperience(context.State.Player, "Farming", 5);
                return true;
            }
            return false;
        }

        private static bool HandleWatering(GameContext context, Tile tile, Item can)
        {
            if (can.Durability > 0 && tile.Type == TileType.TilledDirt)
            {
                tile.Moisture = 1.0f;
                ProgressionManager.AddExperience(context.State.Player, "Farming", 2);
                return true;
            }
            return false;
        }

        private static bool AddItemToInventory(PlayerData player, string name, int count)
        {
            var existing = player.Inventory.FirstOrDefault(i => i.Name == name);
            if (existing != null) {
                existing.Count += count;
                return true;
            }
            
            if (player.Inventory.Count < player.InventorySlots) {
                player.Inventory.Add(new Item { Name = name, Type = "Resource", Count = count });
                return true;
            }
            
            return false;
        }
    }
}
