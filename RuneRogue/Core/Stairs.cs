using RLNET;
using RogueSharp;
using RuneRogue.Interfaces;

namespace RuneRogue.Core
{
    public class Stairs : IDrawable
    {
        public RLColor Color
        {
            get; set;
        }
        public char Symbol
        {
            get; set;
        }
        public int X
        {
            get; set;
        }
        public int Y
        {
            get; set;
        }
        public bool IsUp
        {
            get; set;
        }

        public void Draw(RLConsole console, IMap map)
        {
            if (!map.GetCell(X, Y).IsExplored)
            {
                return;
            }

            Symbol = IsUp ? '<' : '>';


            if (map.IsInFov(X, Y))
            {
                Color = Colors.Player;
            }
            else
            {
                Color = Colors.FloorFov;
            }

            // stairs down are 'throne' on max level
            if (!IsUp && Game.mapLevel == Game.MaxDungeonLevel)
            {
                Symbol = '&';
                Color = Colors.Gold;
            }

            console.Set(X, Y, Color, null, Symbol);
        }
    }
}
