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
            "none",
            "boss",
            "brute",
            "elite",
            "shooter",
            "skirmisher",
            "sneak"
        };

        public static readonly Dictionary<string, int> RolesShootPropensity = new Dictionary<string, int>()
        {
            ["none"] = 50,
            ["boss"] = 50,
            ["brute"] = 20,
            ["elite"] = 30,
            ["shooter"] = 60,
            ["skirmisher"] = 40,
            ["sneak"] = 50
        };

        public static readonly Dictionary<string, int> RolesWanderPropensity = new Dictionary<string, int>()
        {
            ["none"] = 0,
            ["boss"] = 0,
            ["brute"] = 20,
            ["elite"] = 0,
            ["shooter"] = 30,
            ["skirmisher"] = 0,
            ["sneak"] = 50
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

        private string _attack;
        private string _weaponSkill;
        private string _awareness;
        private string _color;
        private string _armor;
        private string _dodgeSkill;
        private string _gold;
        private string _maxHealth;
        private string _speed;

        public string Kind { get; set; }
        public string BaseKind { get; set; }
        public string Attack 
        { 
            get { return (BaseType != null) ? CombineDiceStrings(_attack, BaseType.Attack) : _attack; }
            set { _attack = value; } 
        }
        public string WeaponSkill 
        {
            get { return (BaseType != null) ? CombineDiceStrings(_weaponSkill, BaseType.WeaponSkill) : _weaponSkill; }
            set { _weaponSkill = value; }
        }
        public string Awareness 
        { 
            get { return (BaseType != null) ? CombineDiceStrings(_awareness, BaseType.Awareness) : _awareness; }
            set { _awareness = value; }
        }
        public string Color 
        { 
            get { return _color ?? BaseType.Color; }
            set { _color = value; } 
        }
        public string Armor 
        { 
            get { return (BaseType != null) ? CombineDiceStrings(_armor, BaseType.Armor) : _armor; }
            set { _armor = value; }
        }
        public string DodgeSkill 
        { 
            get { return (BaseType != null) ? CombineDiceStrings(_dodgeSkill, BaseType.DodgeSkill) : _dodgeSkill; }
            set { _dodgeSkill = value; }
        }
        public string Gold 
        {
            get { return (BaseType != null) ? CombineDiceStrings(_gold, BaseType.Gold) : _gold; }
            set { _gold = value; }
        }
        public string MaxHealth 
        { 
            get { return (BaseType != null) ? CombineDiceStrings(_maxHealth, BaseType.MaxHealth) : _maxHealth; }
            set { _maxHealth = value; }
        }
        public string Speed {
            get { return (BaseType != null) ? CombineDiceStrings(_speed, BaseType.Speed) : _speed; }
            set { _speed = value; }
        }
        public int MissileAttack { get { return Int32.Parse(RangedAttackMissile[3]); } }
        public int MissileRange { get { return Int32.Parse(RangedAttackMissile[2]); } }
        public string MissileType { get { return RangedAttackMissile[1]; } }
        public int SpecialAttackRange { get { return Int32.Parse(RangedAttackSpecial[2]); } }
        public string SpecialAttackType { get { return RangedAttackSpecial[1]; } }
        public string Name { get; set; }
        public string Role
        {
            get { return _role ?? "none"; }
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
            get { return _minLevel ?? (BaseType != null ? BaseType.MinLevel : -666); }
            set { _minLevel = value; } 
        }
        public int MaxLevel {
            get { return _maxLevel ?? (BaseType != null ? BaseType.MaxLevel : -666); }
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
        // applies role adjustments
        public void Init()
        {
            NumberKilled = 0;
            NumberGenerated = 0;

            // apply modifiers for role if monster kind is derived from some other kind
            if (BaseKind != null)
            {
                switch (Role.ToLower())
                {
                    case "none":
                        break;
                    case "sneak":
                        Attack += "+1";
                        DodgeSkill += "+1";
                        break;
                    case "boss":
                        Attack += "+2";
                        Armor += "+2";
                        WeaponSkill += "+2";
                        DodgeSkill += "+2";
                        Speed += "+3";
                        MaxHealth += "+2d6k1";
                        break;
                    case "shooter":
                        WeaponSkill = "-2";
                        DodgeSkill += "+2";
                        break;
                    case "elite":
                        Attack += "+2";
                        Armor += "+1";
                        WeaponSkill += "+2";
                        DodgeSkill += "+2";
                        Speed += "+2";
                        break;
                    case "skirmisher":
                        DodgeSkill += "+2";
                        Speed += "+3";
                        break;
                    case "brute":
                        Attack += "+3";
                        WeaponSkill += "+2";
                        DodgeSkill += "-2";
                        MaxHealth += "+2d4k1";
                        break;
                }
            }
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
                    _rangedAttacksMissile = new List<string>() { "none", "none", "0", "0" };
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
                    _rangedAttacksSpecial = new List<string>() { "none", "none", "0", "0" };
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
            if (!(FollowerKinds == null && FollowerNumberAppearing == null && FollowerProbability == null))
            {
                if (FollowerKinds == null || FollowerNumberAppearing == null || FollowerProbability == null)
                {
                    throw new MonsterDataFormatInvalid($"One or more {Kind} follower arrays undefined.");
                }
                bool followerFormatBad =
                    (!(FollowerKinds.Length == FollowerNumberAppearing.Length) ||
                    !(FollowerKinds.Length == FollowerProbability.Length));
                if (followerFormatBad)
                {
                    throw new MonsterDataFormatInvalid($"{Kind} follower arrays of different lengths.");
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
            if (RangedAttackMissile[0] != "none")
            {
                if (!_allRangedAttacksMissile.Contains(RangedAttackMissile[1]))
                {
                    throw new MonsterDataFormatInvalid($"{Kind} has invalid missile weapon type {RangedAttackMissile[1]}.");
                }
                if (!Int32.TryParse(RangedAttackMissile[2], out int _))
                {
                    throw new MonsterDataFormatInvalid($"{Kind} has invalid missile weapon range {RangedAttackMissile[2]}.");
                }
                if (!Int32.TryParse(RangedAttackMissile[3], out int _))
                {
                    throw new MonsterDataFormatInvalid($"{Kind} has invalid missile weapon attack {RangedAttackMissile[3]}.");
                }
            }
            if (RangedAttackSpecial[0] != "none")
            {
                if (!_allRangedAttacksSpecial.Contains(RangedAttackSpecial[1]))
                {
                    throw new MonsterDataFormatInvalid($"{Kind} has invalid special ranged attack {RangedAttackSpecial[1]}.");
                }
                if (!Int32.TryParse(RangedAttackSpecial[2], out int _))
                {
                    throw new MonsterDataFormatInvalid($"{Kind} has invalid special ranged attack range value {RangedAttackSpecial[2]}.");
                }
            }
            if (!_allRoles.Contains(Role.ToLower()))
            {
                throw new MonsterDataFormatInvalid($"{Kind} has invalid role {Role}.");
            }
            MonsterStats monsterType = this;
            MonsterStats baseType = Game.MonsterGenerator.FiendFolio[this.BaseKind ?? this.Kind];
            if (monsterType.Name == null || monsterType.Name == "")
            {
                throw new MonsterDataFormatInvalid($"{Kind} is missing name.");
            }
            CheckStatDefined(monsterType.Attack, "Attack");
            CheckStatDefined(monsterType.WeaponSkill, "WeaponSkill");
            CheckStatDefined(monsterType.Awareness,  "Awareness");
            CheckStatDefined(monsterType.Armor, "Armor");
            CheckStatDefined(monsterType.DodgeSkill, "DodgeSkill");
            CheckStatDefined(monsterType.Gold, "Gold");
            CheckStatDefined(monsterType.MaxHealth, "MaxHealth");
            CheckStatDefined(monsterType.Speed, "Speed");
            CheckStatDefined(monsterType.NumberAppearing,  "NumberAppearing");
            CheckStatDefined(monsterType.MinLevel, "MinLevel");
            CheckStatDefined(monsterType.MaxLevel, "MaxLevel");
            int nameChars = Game.StatWidth - 4;
            if (monsterType.Name.Length > nameChars)
            {
                throw new MonsterDataFormatInvalid($"{Kind} name is too long ({monsterType.Name.Length}" +
                    $" characters > {nameChars} max).");
            }
        }

        private void CheckStatDefined(int kindRoll, string stat)
        {
            if (kindRoll == -666)
            {
                throw new MonsterDataFormatInvalid($"{Kind} missing stat {stat}.");
            }
        }

        private void CheckStatDefined(int kindRoll, int baseRoll, string stat)
        {
            if (kindRoll == -666 && baseRoll == -666)
            {
                throw new MonsterDataFormatInvalid($"{Kind} missing stat {stat}.");
            }
        }

        private void CheckStatDefined(string kindRoll, string stat)
        {
            if (kindRoll == null)
            {
                throw new MonsterDataFormatInvalid($"{Kind} missing stat {stat}.");
            }
            try
            {
                int _ = Dice.Roll(kindRoll);
            }
            catch
            {
                throw new MonsterDataFormatInvalid($"{Kind} stat {stat} mispecified (is {kindRoll}).");
            }
        }

        private void CheckStatDefined(string kindRoll, string baseRoll, string stat)
        {
            int testRoll;
            if (kindRoll == null && baseRoll == null)
            {
                throw new MonsterDataFormatInvalid($"{Kind} missing stat {stat}.");
            }
            try
            {
                testRoll = CombinedDiceRoll(kindRoll, baseRoll);
            }
            catch
            {
                throw new MonsterDataFormatInvalid($"{Kind} stat {stat} mispecified.");
            }
        }

        private string CombineDiceStrings(string kindRoll, string baseRoll)
        {
            // if kindRoll defined and begins with + or -, add that to base roll
            // and roll. Otherwise, if kind roll defined, roll that, and if
            // not, roll base roll
            char firstChar = (kindRoll != null) ? kindRoll.TrimStart()[0] : ' ';
            if (firstChar == '+' || firstChar == '-')
            {
                return (baseRoll + kindRoll);
            }
            return (kindRoll ?? baseRoll);
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
                CanSeePlayer = false,

                Attack = Dice.Roll(monsterType.Attack),
                WeaponSkill = Dice.Roll(monsterType.WeaponSkill) / 10,
                Awareness = Dice.Roll(monsterType.Awareness),
                Armor = Dice.Roll(monsterType.Armor),
                DodgeSkill = Dice.Roll(monsterType.DodgeSkill) / 10,
                Gold = Dice.Roll(monsterType.Gold),
                MaxHealth = Dice.Roll(monsterType.MaxHealth),
                Speed = Dice.Roll(monsterType.Speed),

                MissileAttack = monsterType.MissileAttack,
                MissileRange = monsterType.MissileRange,
                MissileType = monsterType.MissileType,
                SpecialAttackRange = monsterType.SpecialAttackRange,
                SpecialAttackType = monsterType.SpecialAttackType,

                ShootPropensity = RolesShootPropensity[Role.ToLower()],
                WanderPropensity = RolesWanderPropensity[Role.ToLower()],

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

            monster.Health = monster.MaxHealth;
            return monster;
        }
    }

}
