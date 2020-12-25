using System;
using RLNET;
using RuneRogue.Behaviors;
using RuneRogue.Interfaces;
using RuneRogue.Systems;
using RogueSharp;

namespace RuneRogue.Core
{
    public class Monster : Actor, INPC
    {
        public int? TurnsAlerted { get; set; }
        public Cell LastLocationPlayerSeen { get; set; }

        private string _numberAppearing;
        private int _minLevel;
        private int _maxLevel;
        private string[] _followerKinds;
        private string[] _followerNumberAppearing;
        private int[] _followerProbability;
        private int _encounterRarity;

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
        public string[] FollowerKinds
        {
            get { return _followerKinds; }
            set { _followerKinds = value; }
        }
        public string[] FollowerNumberAppearing
        {
            get { return _followerNumberAppearing; }
            set { _followerNumberAppearing = value; }
        }
        public int[] FollowerProbability
        {
            get { return _followerProbability; }
            set { _followerProbability = value; }
        }
        public int EncounterRarity
        {
            get { return _encounterRarity; }
            set { _encounterRarity = value; }
        }



        public void DrawStats(RLConsole statConsole, int position, bool highlight=false)
        {
            // Start at Y=28 which is below the player stats.
            // Multiply the position by 2 to leave a space between each stat
            int yPosition = Game.Player.StatLines + (position * 2);
            // Begin the line by printing the symbol of the monster in the appropriate color
            statConsole.Print(1, yPosition, Symbol.ToString(), Color);
            // Figure out the width of the health bar by dividing current health by max health
            int width = Convert.ToInt32(((double)Health / (double)MaxHealth) * (double)(Game.StatWidth - 4));
            int remainingWidth = Game.StatWidth - 5 - width;
            // Set the background colors of the health bar to show how damaged the monster is
            RLColor healthBarColor = Swatch.PrimaryLighter;
            if (this.IsPoisoned)
            {
                healthBarColor = Swatch.DbGrass;
            }
            else if (this.SARegeneration && Health < MaxHealth)
            {
                healthBarColor = Swatch.DbVegetation;
            }
            //else if (this.IsInvisible)
            //{
            //    healthBarColor = Swatch.DbDark;
            //}
            statConsole.SetBackColor(3, yPosition, width, 1, healthBarColor);
            statConsole.SetBackColor(3 + width, yPosition, remainingWidth, 1, Swatch.PrimaryDarkest);
            // Print the monsters name over top of the health bar
            if (highlight)
            {
                statConsole.Print(2, yPosition, $": ", Swatch.DbLight);
                statConsole.Print(4, yPosition, $"{Name}", Swatch.DbLight, Colors.Gold);
            }
            else
            {
                statConsole.Print(2, yPosition, $": {Name}", Swatch.DbLight);
            }
        }

        public void PerformAction(CommandSystem commandSystem)
        {
            var behavior = new StandardMoveAndAttack();
            behavior.Act(this, commandSystem);
        }
    }
}
