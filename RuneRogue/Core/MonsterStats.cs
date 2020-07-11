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

        public bool LifedrainOnDamage { get; set; }
        public bool LifedrainOnHit { get; set; }
        public bool Regeneration { get; set; }
       
    }

}
