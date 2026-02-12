namespace FarmServer
{
    public class GameContext
    {
        public GameState State { get; private set; }
        public bool IsDirty { get; set; } = false; // Flaga oznaczająca, że stan wymaga zapisu

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
