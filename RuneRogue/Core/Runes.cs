using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RLNET;
using RogueSharp.DiceNotation;
using RuneRogue.Interfaces;

namespace RuneRogue.Core
{
    public class Runes : SecondaryConsole
    {


        private readonly string[] _runeNames =
        {
            "Life",
            "Death",
            "Elements",
            "Thought",
            "Time",
            "Magic",
            "Iron",
            "Darkness"
        };

        // x out of 1000
        private readonly int[] _runeDecayProbabilities =
        {
            20,
            500,
            500,
            12,
            15,
            500,
            15,
            30
        };

        public const int BonusToAttackIron = 6;
        public const int BonusToDefenseIron = 4;
        public const int BonusToSpeedTime = 4;

        private Dictionary<string, int> _decayProbability;
        private List<string> _runesOwned;
        private List<string> _runesActive;

        public Runes()
        {
            _numOptions = _runeNames.Length;
            _runesOwned = new List<string>();
            _runesActive = new List<string>();
            
            _decayProbability = new Dictionary<string, int>();
            for (int i = 0; i < _runeNames.Length; i++)
            {
                _decayProbability.Add(_runeNames[i], _runeDecayProbabilities[i]);
            }

            //AcquireRune("Elements");
            //AcquireRune("Death");
            //AcquireRune("Iron");
            //AcquireRune("Magic");
        }


        public void AcquireRune(string rune)
        {
            List<string> existingRunes = new List<string>(_runeNames);
            if (existingRunes.Contains(rune) && !_runesOwned.Contains(rune))
            {
                _runesOwned.Add(rune);
            }
            else
            {
                throw new ArgumentException($"Invalid rune {rune}.", "rune");
            }
        }

        // checks decay of a non-continuous use, discrete use rune.
        // returns true if decay happened.
        public bool CheckDecay(string rune)
        {
            if (Dice.Roll("1d1000") < _decayProbability[rune])
            {
                if (_runesOwned.Contains("Magic"))
                {
                    Game.MessageLog.Add($"{Game.Player.Name}'s Rune of {rune} trembles.");
                    Game.MessageLog.Add($"{Game.Player.Name}'s Rune of Magic turns to dust.");
                    _runesOwned.Remove("Magic");
                    return false;
                }
                else
                {
                    Game.MessageLog.Add($"{Game.Player.Name}'s Rune of {rune} turns to dust.");
                    _runesOwned.Remove(rune);
                    return true;
                }
            }
            return false;
        }

        // check decay of all active runes
        public void CheckDecayAllRunes()
        {
            List<string> deactivate = new List<string>();
            foreach (string rune in _runesActive)
            {
                if (CheckDecay(rune))
                {
                    deactivate.Add(rune);
                }
            }
            foreach (string rune in deactivate)
            {
                StopRune(rune);
                _runesOwned.Remove(rune);
            }
        }
        
        // returns true if successful
        public bool ToggleRune(string rune)
        {
            if (_runesOwned.Contains(rune) && !(_runesActive.Contains(rune)))
            {
                if (rune == "Magic")
                {
                    Game.MessageLog.Add("The Rune of Magic cannot be activated.");
                    return false;
                }

                Game.MessageLog.Add($"{Game.Player.Name} channels Rune of {rune}.");

                if (rune == "Elements")
                {
                    CheckDecay(rune);
                    Game.SecondaryConsoleActive = true;
                    Game.AcceleratePlayer = false;
                    Game.CurrentSecondary = Game.TargetingSystem;
                    Game.TargetingSystem.InitializeNewTarget("line", "Elements", 8);
                    Game.MessageLog.Add("Select your target.");
                    return false;
                }
                else if (rune == "Death")
                {
                    CheckDecay(rune);
                    Game.SecondaryConsoleActive = true;
                    Game.AcceleratePlayer = false;
                    Game.CurrentSecondary = Game.TargetingSystem;
                    Game.TargetingSystem.InitializeNewTarget("ball", "Death", 8, 5);
                    Game.MessageLog.Add("Select your target.");
                    return false;
                }

                StartRune(rune);

                return true;
            }
            else if (_runesOwned.Contains(rune) && _runesActive.Contains(rune))
            {
                Game.MessageLog.Add($"{Game.Player.Name} stops channeling Rune of {rune}.");

                StopRune(rune);

                return true;
            }
            else
            {
                Game.MessageLog.Add($"{Game.Player.Name} does not have Rune of {rune}.");
                return false;
            }

        }

        public void StartRune(string rune)
        {
            _runesActive.Add(rune);
            switch (rune)
            {
                case "Life":
                    Game.MessageLog.Add($"{Game.Player.Name} begins to regenerate.");
                    Game.Player.SARegeneration = true;
                    break;
                case "Thought":
                    Game.MessageLog.Add($"{Game.Player.Name} begins to sense nearby thoughts.");
                    Game.Player.SASenseThoughts = true;
                    break;
                case "Time":
                    Game.MessageLog.Add($"{Game.Player.Name} begins to move faster.");
                    Game.Player.Speed -= BonusToSpeedTime;
                    break;
                case "Iron":
                    Game.MessageLog.Add($"{Game.Player.Name}'s equipment transforms into adamant.");
                    Game.Player.Attack += BonusToAttackIron;
                    Game.Player.Defense += BonusToDefenseIron;
                    break;
                case "Darkness":
                    Game.MessageLog.Add("not implemented");
                    break;
                //case "Law":
                //    Game.MessageLog.Add("not implemented");
                //    break;
                //case "Chaos":
                //    Game.MessageLog.Add("not implemented");
                //    break;
                default:
                    throw new ArgumentException($"Rune {rune} start does not exist.");
            }
        }

        public void StopRune(string rune)
        {
            _runesActive.Remove(rune);
            switch (rune)
            {
                case "Life":
                    Game.MessageLog.Add($"{Game.Player.Name} is not longer regenerating.");
                    Game.Player.SARegeneration = false;
                    break;
                case "Thought":
                    Game.MessageLog.Add($"{Game.Player.Name} no longer senses thoughts.");
                    Game.Player.SASenseThoughts = false;
                    break;
                case "Time":
                    Game.MessageLog.Add($"{Game.Player.Name} no longer moves quickly.");
                    Game.Player.Speed += BonusToSpeedTime;
                    break;
                case "Iron":
                    Game.MessageLog.Add($"{Game.Player.Name}'s equipment transforms back to normal.");
                    Game.Player.Attack -= BonusToAttackIron;
                    Game.Player.Defense -= BonusToDefenseIron;
                    break;
                case "Darkness":
                    Game.MessageLog.Add("not implemented");
                    break;
                default:
                    throw new ArgumentException($"Rune {rune} stop does not exist.");
            }
        }

        public bool AllRunesOwned
        {
            get { return (RunesNotOwned().Count == 0); }
        }

        public bool RuneActive(string rune)
        {
            return _runesActive.Contains(rune);
        }

        public List<string> RunesNotOwned()
        {
            List<string> runeList = new List<string>();
            for (int i = 0; i < _runeNames.Length; i++)
            {
                if (!_runesOwned.Contains(_runeNames[i]))
                {
                    runeList.Add(_runeNames[i]);
                }
            }

            return runeList;
        }

        public override void DrawConsole()
        {
            _console.Clear();
            _console.SetBackColor(0, 0, Game.MapWidth, Game.MapHeight, Swatch.Compliment);

            int descriptionOffset = 0;
            _console.Print(_horizontalOffset, _verticalOffset, $"{Game.Player.Name}'s Necklace of Runes", Colors.TextHeading);
            descriptionOffset += 4;

            for (int i = 0; i < _runeNames.Count(); i++)
            {
                int displayNumber = i + 1;
                string longNameOfRune = "Rune of " + _runeNames[i];
                string nameOfRune = _runeNames[i];
                _console.Print(_horizontalOffset, descriptionOffset + _verticalOffset + 2 * i,
                    "(" + displayNumber.ToString() + ")", Colors.Text);
                if (_runesOwned.Contains(nameOfRune))
                {
                    _console.Print(_horizontalOffset + 4, descriptionOffset + _verticalOffset + 2 * i, longNameOfRune, Colors.Text);
                    if (_runesActive.Contains(nameOfRune))
                    {
                        _console.Print(_horizontalOffset + 72, descriptionOffset + _verticalOffset + 2 * i, "ACTIVE", Colors.TextHeading);
                    }
                }
                else
                {
                    _console.Print(_horizontalOffset + 4, descriptionOffset + _verticalOffset + 2 * i, longNameOfRune, Colors.TextInactive);
                }
            }
            _console.Print(_horizontalOffset, descriptionOffset + 1 + _verticalOffset + 2 * _runeNames.Count(),
                "( X ) Cancel.", Colors.Text);
        }

        public override bool ProcessChoice(int choiceIndex)
        {
            string choice = _runeNames[choiceIndex];
            bool success = ToggleRune(choice);
            return success;
        }
}

}
