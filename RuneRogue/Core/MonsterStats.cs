using System;

namespace RuneRogue.Core
{


    public class MonsterStats
    {
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
        public string Name { get; set; }
        public int Speed { get; set; }
        public char Symbol { get; set; }
        public string NumberAppearing { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }

        public bool SALifedrainOnDamage { get; set; }
        public bool SALifedrainOnHit { get; set; }
        public bool SARegeneration { get; set; }
        public bool SADoppelganger { get; set; }
        public bool SAVampiric { get; set; }
        public bool SAHighImpact { get; set; }
        public string[] FollowerKinds { get; set; }
        public string[] FollowerNumberAppearing { get; set; }
        public int[] FollowerProbability { get; set; }
        public int EncounterRarity { get; set; }
    }

}
