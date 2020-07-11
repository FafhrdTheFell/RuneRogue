using System;
using RLNET;
using RuneRogue.Behaviors;
using RuneRogue.Interfaces;
using RuneRogue.Systems;

namespace RuneRogue.Core
{
    public class Monster : Actor, INPC
    {
        public int? TurnsAlerted { get; set; }

        private string _numberAppearing;
        private int _minLevel;
        private int _maxLevel;

        public string NumberAppearing
        {
            get
            {
                return _numberAppearing;
            }
            set
            {
                _numberAppearing = value;
            }
        }
        public int MinLevel
        {
            get
            {
                return _minLevel;
            }
            set
            {
                _minLevel = value;
            }
        }
        public int MaxLevel
        {
            get
            {
                return _maxLevel;
            }
            set
            {
                _maxLevel = value;
            }
        }

        public void DrawStats(RLConsole statConsole, int position)
        {
            // Start at Y=24 which is below the player stats.
            // Multiply the position by 2 to leave a space between each stat
            int yPosition = 24 + (position * 2);
            // Begin the line by printing the symbol of the monster in the appropriate color
            statConsole.Print(1, yPosition, Symbol.ToString(), Color);
            // Figure out the width of the health bar by dividing current health by max health
            int width = Convert.ToInt32(((double)Health / (double)MaxHealth) * 16.0);
            int remainingWidth = 16 - width;
            // Set the background colors of the health bar to show how damaged the monster is
            statConsole.SetBackColor(3, yPosition, width, 1, Swatch.PrimaryLighter);
            statConsole.SetBackColor(3 + width, yPosition, remainingWidth, 1, Swatch.PrimaryDarkest);
            // Print the monsters name over top of the health bar
            statConsole.Print(2, yPosition, $": {Name}", Swatch.DbLight);
        }

        public virtual void PerformAction(CommandSystem commandSystem)
        {
            var behavior = new StandardMoveAndAttack();
            behavior.Act(this, commandSystem);
        }
    }
}
