using System;
using System.Collections.Generic;
using System.Linq;
using RLNET;
using RogueSharp.DiceNotation;
using RuneRogue.Systems;

namespace RuneRogue.Core
{
    public class RunesConsole : ChoiceConsole
    {

       
        public RunesConsole()
        {
            _numOptions = Game.RuneSystem.AllRunes.Count();
            _choiceDescription = String.Format("{0}'s Necklace of Runes", Game.Player.Name);
        }

        public override List<string> MenuOptions()
        {
            List<string> options = new List<string>(Game.RuneSystem.AllRunes);
            options.ForEach(s => s = "Rune of " + s);
            return options;
        }

        public override List<string> AdditionalDetails()
        {
            List<string> details = new List<string>();
            foreach (string rune in Game.RuneSystem.AllRunes)
            {
                if (Game.RuneSystem.RuneActive(rune))
                {
                    details.Add("ACTIVE");
                }
                else
                {
                    details.Add("");
                }
            }
            return details;
        }

        public override List<int> OptionsInactive()
        {
            List<int> inactive = new List<int>();
            List<string> runesNotOwned = Game.RuneSystem.RunesNotOwned();
            foreach (string rune in Game.RuneSystem.AllRunes)
            {
                if (runesNotOwned.Contains(rune))
                {
                    inactive.Add(Game.RuneSystem.AllRunes.IndexOf(rune));
                }
            }
            return inactive;
        }

        public override bool ProcessChoice(int choiceIndex)
        {
            string choice = Game.RuneSystem.AllRunes[choiceIndex];
            bool success = Game.RuneSystem.ToggleRune(choice);
            return success;
        }
}

}
