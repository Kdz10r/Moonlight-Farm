using System;
using MoonlightFarm.Server.Models;

namespace MoonlightFarm.Server.Services
{
    public class ProgressionManager
    {
        public static void AddExperience(PlayerData player, string skillName, int amount)
        {
            if (player.Skills.ContainsKey(skillName))
            {
                int oldExp = player.Skills[skillName];
                int newExp = oldExp + amount;
                player.Skills[skillName] = newExp;

                // Sprawdź awans na poziom (co 100 punktów expa)
                int oldLevel = oldExp / 100;
                int newLevel = newExp / 100;

                if (newLevel > oldLevel)
                {
                    OnLevelUp(player, skillName, newLevel);
                }
            }
        }

        private static void OnLevelUp(PlayerData player, string skillName, int level)
        {
            // Bonusy za poziom
            player.MaxEnergy += 5; // Każdy poziom daje +5 energii
            player.Energy = player.MaxEnergy;

            // Unlock recipes/items
            if (skillName == "Farming")
            {
                if (level == 2) player.Inventory.Add(new Item { Name = "Moon Blossom Seeds", Type = "Seed", Count = 5 });
                if (level == 5) player.Inventory.Add(new Item { Name = "Silver Wheat Seeds", Type = "Seed", Count = 5 });
            }
            else if (skillName == "Mining")
            {
                if (level == 3) player.Inventory.Add(new Item { Name = "Steel Pickaxe", Type = "Tool", Durability = 200 });
            }

            Console.WriteLine($"Skill {skillName} leveled up to {level}!");
        }

        public static int GetLevel(PlayerData player, string skillName)
        {
            if (player.Skills.TryGetValue(skillName, out int exp))
            {
                return (exp / 100) + 1;
            }
            return 1;
        }
    }
}
