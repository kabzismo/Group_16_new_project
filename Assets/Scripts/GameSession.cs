namespace FPSStarter
{
    public static class GameSession
    {
        public static int CollectedItems { get; set; }

        public static void Reset()
        {
            CollectedItems = 0;
        }
    }
}
