using System;
using System.Collections.Generic;
using System.Linq;
using MoonlightFarm.Server.Models;

namespace MoonlightFarm.Server.Services
{
    public class NPCManager
    {
        private static readonly Random _random = new Random();

        public static string GetDialogue(NPCData npc, GameState state)
        {
            var time = state.Time;
            
            // Specjalne dialogi podczas pełni
            if (time.IsFullMoon)
            {
                return npc.Type switch
                {
                    "Magic" => "Księżyc w pełni odsłania ścieżki, których nie widać za dnia. Czy odwiedziłeś już Moonlight Grove?",
                    "Smith" => "W taką noc srebro samo się kuje. Czy potrzebujesz czegoś wyjątkowego?",
                    "Merchant" => "Dziś w karczmie serwujemy Księżycowy Napar! Specjalność zakładu.",
                    _ => "Ten blask... czujesz to mrowienie na skórze?"
                };
            }

            // Dialogi zależne od pogody
            if (state.Weather == WeatherType.Rain || state.Weather == WeatherType.Storm)
            {
                return npc.Type switch
                {
                    "Smith" => "Dobra pogoda na siedzenie przy kuźni. Przynajmniej tu jest sucho.",
                    "Merchant" => "Nikt nie chce pić w taką ulewę. Może ty zostaniesz na chwilę?",
                    _ => "Brrr, ale leje."
                };
            }

            // Dialogi zależne od pory dnia
            if (time.IsNight)
            {
                return npc.Type switch
                {
                    "Magic" => "Gwiazdy są dziś wyjątkowo rozmowne. Słyszysz ich szept?",
                    "Merchant" => "Już prawie zamykamy, ale dla stałego klienta zawsze znajdzie się miejsce.",
                    _ => "Trochę późno na odwiedziny, nie sądzisz?"
                };
            }

            // Dialogi zależne od poziomu przyjaźni
            if (npc.Friendship > 500)
            {
                return npc.Type switch
                {
                    "Smith" => "Dobrze cię widzieć! Twoje narzędzia sprawują się bez zarzutu?",
                    "Merchant" => "O, mój ulubiony klient! Mam dla ciebie specjalną zniżkę w pamięci.",
                    "Magic" => "Widzę w twojej aurze wielki spokój. Farma ci służy.",
                    _ => "Witaj, przyjacielu!"
                };
            }

            // Dialogi standardowe
            return npc.Type switch
            {
                "Smith" => "Jeśli potrzebujesz lepszych narzędzi, przynieś mi rudę i trochę złota.",
                "Merchant" => "Witaj w karczmie! Mam najlepsze trunki w okolicy.",
                "Magic" => "Gwiazdy mówią o wielkich zmianach na twojej farmie.",
                _ => "Dzień dobry!"
            };
        }

        public static bool TalkToNPC(PlayerData player, NPCData npc, int currentDay)
        {
            if (npc.LastTalkedDay != currentDay)
            {
                npc.Friendship = Math.Min(1000, npc.Friendship + 20);
                npc.LastTalkedDay = currentDay;
                return true;
            }
            return false;
        }

        public static (bool Success, string Message) GiveGift(PlayerData player, NPCData npc, Item item)
        {
            if (item == null) return (false, "Nie masz nic do dania!");

            // Sprawdź czy NPC lubi ten przedmiot (uproszczone)
            int points = 0;
            string reaction = "";

            if (item.Type == "Fish")
            {
                points = 50;
                reaction = "Och, świeża ryba! Dziękuję.";
            }
            else if (item.Type == "Seed")
            {
                points = 10;
                reaction = "Nasiona? Może się przydadzą.";
            }
            else
            {
                points = 20;
                reaction = "Dziękuję za prezent!";
            }

            npc.Friendship = Math.Min(1000, npc.Friendship + points);
            
            // Usuń przedmiot z ekwipunku
            if (item.Count > 1) item.Count--;
            else player.Inventory.Remove(item);

            return (true, reaction);
        }
    }
}
