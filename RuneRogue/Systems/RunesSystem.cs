using System;
using System.Collections.Generic;
using System.Linq;
using RLNET;
using RogueSharp.DiceNotation;
using RuneRogue.Core;

namespace RuneRogue.Systems
{
    public class RunesSystem
    {

        private static readonly string[] _runeNames =
        {
            "Life",
            "Death",
            "Elements",
            "Thought",
            "Time",
            "Magic",
            "Iron",
            "Darkness",
            "Ram"
        };

        public List<string> AllRunes
        {
            get { return new List<string>(_runeNames); }
        }

        public List<string> ActiveRunes
        {
            get { return new List<string>(_runesActive); }
        }

        // x out of 1000
        private static readonly Dictionary<string, int> _decayProbability = new Dictionary<string, int>()
        {
            ["Life"] = 500,
            ["Death"] = 500,
            ["Elements"] = 500,
            ["Thought"] = 10,
            ["Time"] = 15,
            ["Magic"] = 500,
            ["Iron"] = 80,
            ["Darkness"] = 18,
            ["Ram"] = 150
        };

        private static readonly List<string> _passiveRunes = new List<string>()
        {
            "Magic",
            "Life"
        };

        private static readonly List<string> _functionalRunes = new List<string>()
        {
            "Thought",
            "Time",
            "Darkness"
        };

        private static readonly List<string> _offensiveRunes = new List<string>()
        {
            "Elements",
            "Death",
            "Iron",
            "Ram"
        };

        private static readonly Dictionary<string, string> _offensiveProjectile = new Dictionary<string, string>()
        {
            ["Elements"] = "line",
            ["Death"] = "ball",
            ["Iron"] = "missile",
            ["Ram"] = "missile"
        };

        private static readonly Dictionary<string, int> _offensiveRange = new Dictionary<string, int>()
        {
            ["Elements"] = 8,
            ["Death"] = 8,
            ["Iron"] = 10,
            ["Ram"] = 6
        };

        private static readonly Dictionary<string, int> _offensiveMinRange = new Dictionary<string, int>()
        {
            ["Elements"] = 1,
            ["Death"] = 1,
            ["Iron"] = 1,
            ["Ram"] = 3
        };

        private static readonly Dictionary<string, int> _offensiveRadius = new Dictionary<string, int>()
        {
            ["Elements"] = 1,
            ["Death"] = 5,
            ["Iron"] = 1,
            ["Ram"] = 1
        };

        public const int BonusToDamageIron = 18;
        public const int BonusToRamAttack = 9;
        public const int BonusToSpeedTime = 4;
        public const int DistanceSenseThoughts = 18;

        private List<string> _runesOwned;
        private List<string> _runesActive;

        public RunesSystem()
        {
            _runesOwned = new List<string>();
            _runesActive = new List<string>();

            AcquireRune("Thought");
            //AcquireRune("Elements");
            //AcquireRune("Death");
            //AcquireRune("Time");
            AcquireRune("Iron");
            AcquireRune("Magic");
            //AcquireRune("Life");
            //AcquireRune("Ram");
            //AcquireRune("Darkness");
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

        // returns true if decay happened.
        public bool CheckDecay(string rune)
        {
            bool decayed = CheckDecay(rune, out List<string> messages);
            foreach (string s in messages)
            {
                Game.MessageLog.Add(s);
            }
            return decayed;
        }
        public bool CheckDecay(string rune, out List<string> decayMessages)
        {
            decayMessages = new List<string>();
            if (Dice.Roll("1d1000") < _decayProbability[rune])
            {
                if (_runesOwned.Contains("Magic"))
                {
                    decayMessages.Add($"{Game.Player.Name}'s Rune of {rune} trembles.");
                    decayMessages.Add($"{Game.Player.Name}'s Rune of Magic turns to dust.");
                    _runesOwned.Remove("Magic");
                    return false;
                }
                else
                {
                    decayMessages.Add($"{Game.Player.Name}'s Rune of {rune} turns to dust.");
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
                if (_passiveRunes.Contains(rune))
                {
                    Game.MessageLog.Add($"The Rune of {rune} cannot be activated.");
                    return false;
                }

                Game.MessageLog.Add($"{Game.Player.Name} channels Rune of {rune}.");

                if (_offensiveRunes.Contains(rune))
                {
                    Game.SecondaryConsoleActive = true;
                    Game.AcceleratePlayer = false;
                    Game.CurrentSecondary = new TargetingSystem(_offensiveProjectile[rune],
                        _offensiveRange[rune], _offensiveRadius[rune], _offensiveMinRange[rune]);
                    Game.PostSecondary = new Instant(rune, radius: 
                        _offensiveRadius[rune], special: "Rune");
                    Game.MessageLog.Add("Select your target (TAB to cycle).");
                    return false;
                }

                // functional rune
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
                case "Thought":
                    Game.MessageLog.Add($"{Game.Player.Name} begins to sense nearby thoughts.");
                    Game.Player.SASenseThoughts = true;
                    break;
                case "Time":
                    Game.MessageLog.Add($"{Game.Player.Name} begins to move faster.");
                    Game.Player.Speed -= BonusToSpeedTime;
                    break;
                case "Darkness":
                    Game.MessageLog.Add($"{Game.Player.Name} fades into the shadows.");
                    Game.Player.SAStealthy = true;
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
                case "Thought":
                    Game.MessageLog.Add($"{Game.Player.Name} no longer senses thoughts.");
                    Game.Player.SASenseThoughts = false;
                    break;
                case "Time":
                    Game.MessageLog.Add($"{Game.Player.Name} no longer moves quickly.");
                    Game.Player.Speed += BonusToSpeedTime;
                    break;
                case "Darkness":
                    Game.MessageLog.Add($"{Game.Player.Name} is no longer hidden by darkness.");
                    Game.Player.SAStealthy = false;
                    break;
                default:
                    throw new ArgumentException($"Rune {rune} stop does not exist.");
            }
        }

        public bool RuneActive(string rune)
        {
            return _runesActive.Contains(rune);
        }

        public List<string> RunesNotOwned()
        {
            return AllRunes.Except(_runesOwned).ToList();
        }

        public List<string> RunesOwned()
        {
            return new List<string>(_runesOwned);
        }

       
    }

}
