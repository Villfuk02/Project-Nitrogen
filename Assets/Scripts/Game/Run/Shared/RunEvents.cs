using Utils;

namespace Game.Run.Shared
{
    public static class RunEvents
    {
        public static ModifiableCommand<int> damageHull = new();
        public static ModifiableCommand<int> repairHull = new();
        public static ModifiableCommand defeat = new();
        public static ModifiableCommand finishLevel = new();
        public static ModifiableCommand quit = new();

        public static void InitEvents()
        {
            damageHull = new();
            repairHull = new();
            defeat = new();
            finishLevel = new();
            quit = new();
        }
    }
}