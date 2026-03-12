using System.Text.Json.Serialization;

namespace MoonlightFarm.Server.Models
{
    public enum Season
    {
        Spring = 0,
        Summer = 1,
        Autumn = 2,
        Winter = 3
    }

    public class GameTime
    {
        // Całkowita liczba minut, która upłynęła od początku gry
        public long TotalMinutes { get; set; } = 0;

        // Stałe konfiguracyjne czasu (można je przenieść do configu globalnego)
        public const int MinutesInDay = 24 * 60; // 1440
        public const int DaysInSeason = 28;
        public const int SeasonsInYear = 4;
        
        // Pomocnicze właściwości wyliczane (nie muszą być serializowane, ale dla API się przydadzą)
        // W C# System.Text.Json domyślnie serializuje publiczne właściwości
        
        [JsonIgnore]
        public int CurrentYear => (int)(TotalMinutes / (MinutesInDay * DaysInSeason * SeasonsInYear)) + 1;
        
        [JsonIgnore]
        public Season CurrentSeason => (Season)((TotalMinutes / (MinutesInDay * DaysInSeason)) % SeasonsInYear);
        
        [JsonIgnore]
        public int CurrentDay => (int)((TotalMinutes / MinutesInDay) % DaysInSeason) + 1;
        
        [JsonIgnore]
        public int CurrentHour => (int)((TotalMinutes % MinutesInDay) / 60);
        
        [JsonIgnore]
        public int CurrentMinute => (int)(TotalMinutes % 60);

        [JsonIgnore]
        public bool IsNight => CurrentHour >= 20 || CurrentHour < 6;

        public string MoonPhase {
            get {
                int day = CurrentDay;
                if (day <= 7) return "Nów";
                if (day <= 14) return "Pierwsza Kwadra";
                if (day <= 21) return "Pełnia";
                return "Trzecia Kwadra";
            }
        }

        public bool IsFullMoon => CurrentDay > 18 && CurrentDay <= 21;

        // Obiekt do zwracania ładnie sformatowanego czasu w API
        public object DisplayTime => new
        {
            Year = CurrentYear,
            Season = CurrentSeason.ToString(),
            Day = CurrentDay,
            Hour = CurrentHour,
            Minute = CurrentMinute,
            TotalMinutes = TotalMinutes,
            MoonPhase = MoonPhase,
            IsFullMoon = IsFullMoon
        };
    }
}
