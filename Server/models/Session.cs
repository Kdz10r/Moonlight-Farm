namespace MoonlightFarm.Server.Models
{
    public class Session
    {
        public string Id { get; private set; }
        public DateTime LastActive { get; set; }
        public GameContext Context { get; private set; }

        public Session(string id, GameContext context)
        {
            Id = id;
            LastActive = DateTime.UtcNow;
            Context = context;
        }

        public void Touch()
        {
            LastActive = DateTime.UtcNow;
        }
    }
}
