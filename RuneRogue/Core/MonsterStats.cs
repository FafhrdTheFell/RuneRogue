using System;
using System.Collections.Generic;
using System.Linq;
using RuneRogue.Interfaces;

namespace RuneRogue.Core
{

    class MonsterDataFormatInvalid : Exception
    {
        public MonsterDataFormatInvalid(string message)
            : base(message)
        {
        }
    }

    // The MonsterStats class defines a type of monster along with information
    // on followers, dungeon level, and so on (defined in INPC).
    public class MonsterStats : INPC
    {
        private static List<string> _allSpecialAbilities = new List<string>()
        {
            "Regeneration",
            "Sense Thoughts",
            "Stealthy",
            "Vampiric",
            "Venomous",
            "Stuns",
            "Doppelganger",
            "High Impact",
            "Undead",
            "Immobile",
            "Ferocious",
            "Life Drain On Damage",
            "Life Drain On Hit"
        };

        private static List<string> _allRangedAttacks = new List<string>()
        {
            "Arrow",
            "Spit",
            "Boulder",
            "Fire",
            "Deathchant"
        };

        private List<string> _specialAbilities;
        private List<string> _rangedAttacksMissile;
        private List<string> _rangedAttacksSpecial;

        public string Kind { get; set; }
        public string Attack { get; set; }
        public string AttackChance { get; set; }
        public int Awareness { get; set; }
        public string Color { get; set; }
        public string Defense { get; set; }
        public string DefenseChance { get; set; }
        public string Gold { get; set; }
        //public int Health { get; set; }
        public string MaxHealth { get; set; }
        public int MissileAttack { get { return Int32.Parse(RangedAttackMissile[3]); } }
        public int MissileRange { get { return Int32.Parse(RangedAttackMissile[2]); } }
        public string MissileType { get { return RangedAttackMissile[1]; } }
        public int SpecialAttackRange { get { return Int32.Parse(RangedAttackSpecial[2]); } }
        public string SpecialAttackType { get { return RangedAttackSpecial[1]; } }
        public string Name { get; set; }
        public int Speed { get; set; }
        public char Symbol { get; set; }
        public string NumberAppearing { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }

        public string[] FollowerKinds { get; set; }
        public string[] FollowerNumberAppearing { get; set; }
        public int[] FollowerProbability { get; set; }
        public int EncounterRarity { get; set; }
        public string[] Special { get; set; }
        public string[] Ranged { get; set; }

        public List<string> SpecialAbilities
        {
            get
            {
                if (_specialAbilities == null)
                {
                    _specialAbilities = (Special != null) ? new List<string>(Special) : new List<string>();
                }
                return _specialAbilities;
            }
        }

        public List<string> RangedAttackMissile
        {
            get
            {
                if (_rangedAttacksMissile == null && Ranged != null)
                {
                    foreach (string rSpec in Ranged)
                    {
                        string[] rSpecs = rSpec.Split(' ');
                        if (rSpecs[0] == "Missile")
                        {
                            _rangedAttacksMissile = new List<string>(rSpecs);
                        }
                    }
                }
                if (_rangedAttacksMissile == null)
                {
                    _rangedAttacksMissile = new List<string>() { "None", "None", "0", "0" };
                }
                return _rangedAttacksMissile;
            }
        }

        public List<string> RangedAttackSpecial
        {
            get
            {
                if (_rangedAttacksSpecial == null && Ranged != null)
                {
                    foreach (string rSpec in Ranged)
                    {
                        string[] rSpecs = rSpec.Split(' ');
                        if (rSpecs[0] == "Special")
                        {
                            _rangedAttacksSpecial = new List<string>(rSpecs);
                        }
                    }
                }
                if (_rangedAttacksSpecial == null)
                {
                    _rangedAttacksSpecial = new List<string>() { "None", "None", "0", "0" };
                }
                return _rangedAttacksSpecial;
            }
        }

        public void CheckDefinition()
        {
            if (SpecialAbilities.Except(_allSpecialAbilities).Count() > 0)
            {
                string invalidSA = SpecialAbilities.Except(_allSpecialAbilities).First();
                throw new MonsterDataFormatInvalid($"{Kind} has unrecognized SA {invalidSA}.");
            }
            if (!(FollowerKinds == null))
            {
                bool followerFormatBad =
                    (!(FollowerKinds.Length == FollowerNumberAppearing.Length) ||
                    !(FollowerKinds.Length == FollowerProbability.Length));
                if (followerFormatBad)
                {
                    throw new MonsterDataFormatInvalid($"{Name} follower arrays of different lengths.");
                }
                // if own type is follower and has 100% chance of being generated, causes infinite loop.
                // note that an infinite loop also occurs if monster1 is follower of monster2 is follower
                // of monster1, but program does not check for that.
                List<string> mFollowerKinds = new List<string>(FollowerKinds);
                if (mFollowerKinds.Contains(Name))
                {
                    throw new MonsterDataFormatInvalid($"{Name} creates itself as follower," +
                        " risking infinite loop.");
                }
            }
        }

        

        public bool HasSpecialAbility(string ability)
        {
            return SpecialAbilities.Contains(ability);
        }
    }

}
