using System;
using System.Collections.Generic;
using MoonlightFarm.Server.Models;

namespace MoonlightFarm.Server.Services
{
    public class FishingManager
    {
        private static readonly Random _random = new Random();

        public static List<string> GetPossibleFish(GameTime time, WeatherType weather, string location)
        {
            var possibleFish = new List<string>();

            // Ryby sezonowe
            switch (time.CurrentSeason)
            {
                case Season.Spring:
                    possibleFish.Add("Sardine");
                    possibleFish.Add("Anchovy");
                    break;
                case Season.Summer:
                    possibleFish.Add("Tuna");
                    possibleFish.Add("Pufferfish");
                    break;
                case Season.Autumn:
                    possibleFish.Add("Salmon");
                    possibleFish.Add("Tiger Trout");
                    break;
                case Season.Winter:
                    possibleFish.Add("Squid");
                    possibleFish.Add("Halibut");
                    break;
            }

            // Podstawowe ryby
            possibleFish.Add("Carp");
            possibleFish.Add("Sunfish");

            // Ryby zależne od pory dnia
            if (time.IsNight)
            {
                possibleFish.Add("Bream");
                possibleFish.Add("Eel");
            }
            else
            {
                possibleFish.Add("Bass");
                possibleFish.Add("Rainbow Trout");
            }

            // Ryby zależne od pogody
            if (weather == WeatherType.Rain || weather == WeatherType.Storm)
            {
                possibleFish.Add("Catfish");
                possibleFish.Add("Walleye");
            }

            // Ryby magiczne podczas pełni
            if (time.IsFullMoon)
            {
                possibleFish.Add("Lunar Salmon");
                possibleFish.Add("Moonlight Jellyfish");
            }

            return possibleFish;
        }

        public static Item CatchFish(GameContext context)
        {
            var possibleFish = GetPossibleFish(context.State.Time, context.State.Weather, "Lake");
            string fishName = possibleFish[_random.Next(possibleFish.Count)];

            // Losowanie jakości
            int qualityRoll = _random.Next(100);
            string quality = "Normal";
            if (qualityRoll > 90) quality = "Perfect";
            else if (qualityRoll > 70) quality = "Good";

            return new Item
            {
                Name = $"{quality} {fishName}",
                Type = "Fish",
                Count = 1
            };
        }

        public static bool TryAddFishToInventory(PlayerData player, Item fish)
        {
            if (player.Inventory.Count >= player.InventorySlots)
            {
                return false;
            }

            player.Inventory.Add(fish);
            return true;
        }
    }
}
