namespace MoonlightFarm.Server.Database
{
    public class GameContext
    {
        public GameState State { get; private set; }
        public bool IsDirty { get; set; } = false; 

        public GameContext(GameState state)
        {
            State = state;
        }

        public void MarkDirty()
        {
            IsDirty = true;
        }
    }
}
