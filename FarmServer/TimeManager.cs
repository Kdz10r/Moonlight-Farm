namespace FarmServer
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

        // Metoda do spania (przeskok do 6:00 następnego dnia)
        public static void Sleep(GameContext context)
        {
            var time = context.State.Time;
            
            // Oblicz ile minut zostało do końca dnia
            var minutesToday = time.TotalMinutes % GameTime.MinutesInDay;
            var minutesUntilMidnight = GameTime.MinutesInDay - minutesToday;
            
            // Dodaj czas do północy + 6 godzin (360 minut)
            // To prosta implementacja: zawsze budzimy się o 6:00 rano kolejnego dnia
            var minutesToSleep = minutesUntilMidnight + (6 * 60);
            
            time.TotalMinutes += minutesToSleep;
            
            // W przyszłości: regeneracja energii, wzrost roślin, reset dziennych limitów
            
            context.MarkDirty();
        }
    }
}
