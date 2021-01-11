using System;
using System.Collections.Generic;
using System.Linq;
using RogueSharp.DiceNotation;
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
        private static readonly List<string> _allSpecialAbilities = new List<string>()
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

        private static readonly List<string> _allRangedAttacksMissile = new List<string>()
        {
            "Arrow",
            "Spit",
            "Boulder"
        };

        private static readonly List<string> _allRangedAttacksSpecial = new List<string>()
        {
            "Fire",
            "Deathchant",
            "Ram"
        };

        private static readonly List<string> _allRoles = new List<string>()
        {
            "None",
            "Boss",
            "Brute",
            "Elite",
            "Shooter",
            "Skirmisher",
            "Sneak"
        };

        public static readonly Dictionary<string, int> RolesShootPropensity = new Dictionary<string, int>()
        {
            ["None"] = 50,
            ["Boss"] = 50,
            ["Brute"] = 20,
            ["Elite"] = 30,
            ["Shooter"] = 60,
            ["Skirmisher"] = 40,
            ["Sneak"] = 50
        };

        public static readonly Dictionary<string, int> RolesWanderPropensity = new Dictionary<string, int>()
        {
            ["None"] = 0,
            ["Boss"] = 0,
            ["Brute"] = 20,
            ["Elite"] = 0,
            ["Shooter"] = 30,
            ["Skirmisher"] = 0,
            ["Sneak"] = 50
        };

        private List<string> _specialAbilities;
        private List<string> _rangedAttacksMissile;
        private List<string> _rangedAttacksSpecial;

        private char? _symbol;

        private string _numAppearing;
        private string _role;
        private int? _minLevel;
        private int? _maxLevel;
        private int? _rarity;
        private bool? _isUnique;

        public string Kind { get; set; }
        public string BaseKind { get; set; }
        public string Attack { get; set; }
        public string WeaponSkill { get; set; }
        public string Awareness { get; set; }
        public string Color { get; set; }
        public string Armor { get; set; }
        public string DodgeSkill { get; set; }
        public string Gold { get; set; }
        public string MaxHealth { get; set; }
        public int MissileAttack { get { return Int32.Parse(RangedAttackMissile[3]); } }
        public int MissileRange { get { return Int32.Parse(RangedAttackMissile[2]); } }
        public string MissileType { get { return RangedAttackMissile[1]; } }
        public int SpecialAttackRange { get { return Int32.Parse(RangedAttackSpecial[2]); } }
        public string SpecialAttackType { get { return RangedAttackSpecial[1]; } }
        public string Name { get; set; }
        public string Speed { get; set; }
        public string Role
        {
            get { return _role ?? "None"; }
            set { _role = value; }
        }
        public char Symbol
        {
            get { return _symbol ?? BaseType.Symbol; }
            set { _symbol = value; }
        }
        public string NumberAppearing { 
            get { return _numAppearing ?? BaseType.NumberAppearing;  }
            set { _numAppearing = value; }
        }
        public int MinLevel {
            get { return _minLevel ?? BaseType.MinLevel; }
            set { _minLevel = value; } 
        }
        public int MaxLevel {
            get { return _maxLevel ?? BaseType.MaxLevel; }
            set { _maxLevel = value; }
        }
        public int? Rarity
        {
            // if rarity undefined, default value is 0 (ranges from -100 to 99)
            get { return _rarity ?? ((BaseKind != null) ? BaseType.Rarity : 0); }
            set { _rarity = value; }
        }
        public bool IsUnique
        {
            get { return _isUnique ?? false; }
            set { _isUnique = value; }
        }
        // number of instances killed
        public int NumberKilled { get; set; }
        // number of encounters generated
        public int NumberGenerated { get; set; }
        public MonsterStats()
        {
            NumberKilled = 0;
            NumberGenerated = 0;
        }

        public string[] FollowerKinds { get; set; }
        public string[] FollowerNumberAppearing { get; set; }
        public int[] FollowerProbability { get; set; }
        public int EncounterRarity { get; set; }
        public string[] Special { get; set; }
        public string[] Ranged { get; set; }

        public MonsterStats BaseType
        {
            get
            {
                if (BaseKind != null)
                {
                    return Game.MonsterGenerator.FiendFolio[this.BaseKind];
                }
                return null;
            }
        }

        public List<string> SpecialAbilities
        {
            get
            {
                if (_specialAbilities == null)
                {
                    // If Special undefined, and base kind exists, use base kind's Special
                    Special = Special ?? (BaseType != null ? BaseType.Special : Special);
                    _specialAbilities = (Special != null) ? new List<string>(Special) : new List<string>();
                }
                return _specialAbilities;
            }
        }

        public List<string> RangedAttackMissile
        {
            get
            {
                Ranged = Ranged ?? (BaseType != null ? BaseType.Ranged : Ranged);
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
                Ranged = Ranged ?? (BaseType != null ? BaseType.Ranged : Ranged);
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
                if (mFollowerKinds.Contains(Kind))
                {
                    throw new MonsterDataFormatInvalid($"{Kind} creates itself as follower," +
                        " risking infinite loop.");
                }
            }
            if (RangedAttackMissile[0] != "None")
            {
                if (!_allRangedAttacksMissile.Contains(RangedAttackMissile[1]))
                {
                    throw new MonsterDataFormatInvalid($"{Kind} has invalid missile weapon type {RangedAttackMissile[1]}.");
                }
                if (!Int32.TryParse(RangedAttackMissile[2], out int Range))
                {
                    throw new MonsterDataFormatInvalid($"{Kind} has invalid missile weapon range {RangedAttackMissile[2]}.");
                }
                if (!Int32.TryParse(RangedAttackMissile[3], out int Attack))
                {
                    throw new MonsterDataFormatInvalid($"{Kind} has invalid missile weapon attack {RangedAttackMissile[3]}.");
                }
            }
            if (RangedAttackSpecial[0] != "None")
            {
                if (!_allRangedAttacksSpecial.Contains(RangedAttackSpecial[1]))
                {
                    throw new MonsterDataFormatInvalid($"{Kind} has invalid special ranged attack {RangedAttackSpecial[1]}.");
                }
                if (!Int32.TryParse(RangedAttackSpecial[2], out int Range))
                {
                    throw new MonsterDataFormatInvalid($"{Kind} has invalid special ranged attack range value {RangedAttackSpecial[2]}.");
                }
            }
            if (!_allRoles.Contains(Role))
            {
                throw new MonsterDataFormatInvalid($"{Kind} has invalid role {Role}.");
            }
        }

        private int CombinedDiceRoll(string kindRoll, string baseRoll)
        {
            // if kindRoll defined and begins with +, add that to base roll
            // and roll. Otherwise, if kind roll defined, roll that, and if
            // not, roll base roll
            char firstChar = (kindRoll != null) ? kindRoll.TrimStart()[0] : ' ';
            if (firstChar == '+')
            {
                return Dice.Roll(baseRoll + kindRoll);
            }
            return Dice.Roll(kindRoll ?? baseRoll);
        }
        

        public bool HasSpecialAbility(string ability)
        {
            return SpecialAbilities.Contains(ability);
        }

        public Monster CreateMonster()
        {
            MonsterStats monsterType = this;
            MonsterStats baseType = Game.MonsterGenerator.FiendFolio[this.BaseKind ?? this.Kind];
            Monster monster = new Monster()
            {
                Name = monsterType.Name,
                Symbol = monsterType.Symbol,
                Color = Colors.ColorLookup(monsterType.Color ?? baseType.Color),
                MonsterType = this,

                Attack = CombinedDiceRoll(monsterType.Attack, baseType.Attack),
                WeaponSkill = CombinedDiceRoll(monsterType.WeaponSkill, baseType.WeaponSkill) / 10,
                Awareness = CombinedDiceRoll(monsterType.Awareness, baseType.Awareness),
                Armor = CombinedDiceRoll(monsterType.Armor, baseType.Armor),
                DodgeSkill = CombinedDiceRoll(monsterType.DodgeSkill, baseType.DodgeSkill) / 10,
                Gold = CombinedDiceRoll(monsterType.Gold, baseType.Gold),
                MaxHealth = CombinedDiceRoll(monsterType.MaxHealth, baseType.MaxHealth),
                Speed = CombinedDiceRoll(monsterType.Speed, baseType.Speed),

                MissileAttack = monsterType.MissileAttack,
                MissileRange = monsterType.MissileRange,
                MissileType = monsterType.MissileType,
                SpecialAttackRange = monsterType.SpecialAttackRange,
                SpecialAttackType = monsterType.SpecialAttackType,

                ShootPropensity = RolesShootPropensity[Role],
                WanderPropensity = RolesWanderPropensity[Role],

                SAFerocious = monsterType.HasSpecialAbility("Ferocious"),
                SALifedrainOnHit = monsterType.HasSpecialAbility("Life Drain On Hit"),
                SALifedrainOnDamage = monsterType.HasSpecialAbility("Life Drain On Damage"),
                SARegeneration = monsterType.HasSpecialAbility("Regeneration"),
                SASenseThoughts = monsterType.HasSpecialAbility("Sense Thoughts"),
                SAStealthy = monsterType.HasSpecialAbility("Stealthy"),
                SAVampiric = monsterType.HasSpecialAbility("Vampiric"),
                SAVenomous = monsterType.HasSpecialAbility("Venomous"),
                SACausesStun = monsterType.HasSpecialAbility("Stuns"),
                SADoppelganger = monsterType.HasSpecialAbility("Doppelganger"),
                SAHighImpact = monsterType.HasSpecialAbility("High Impact"),
                IsUndead = monsterType.HasSpecialAbility("Undead"),
                IsImmobile = monsterType.HasSpecialAbility("Immobile")
            };

            // apply modifiers for role if monster kind is derived from some other kind
            if (BaseKind != null)
            {
                switch (Role)
                {
                    case "None":
                        break;
                    case "Sneak":
                        monster.Attack += 1;
                        monster.DodgeSkill += 1;
                        break;
                    case "Boss":
                        monster.Attack += 2;
                        monster.Armor += 2;
                        monster.WeaponSkill += 2;
                        monster.DodgeSkill += 2;
                        monster.Speed += 3;
                        monster.MaxHealth = monster.MaxHealth * 3 / 2;
                        break;
                    case "Shooter":
                        monster.WeaponSkill -= 2;
                        monster.DodgeSkill += 2;
                        break;
                    case "Elite":
                        monster.Attack += 2;
                        monster.Armor += 1;
                        monster.WeaponSkill += 2;
                        monster.DodgeSkill += 2;
                        monster.Speed += 2;
                        break;
                    case "Skirmisher":
                        monster.DodgeSkill += 2;
                        monster.Speed += 3;
                        break;
                    case "Brute":
                        monster.Attack += 3;
                        monster.WeaponSkill += 2;
                        monster.DodgeSkill -= 2;
                        monster.MaxHealth = monster.MaxHealth * 4 / 3;
                        break;
                }
            }

            monster.Health = monster.MaxHealth;
            return monster;
        }
    }

}
