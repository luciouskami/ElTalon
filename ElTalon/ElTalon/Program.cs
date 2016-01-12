using LeagueSharp.Common;


namespace ElTalon
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Talon.Game_OnGameLoad;
        }
    }
}