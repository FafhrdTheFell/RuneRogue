using RLNET;
using RogueSharp;
using RuneRogue.Interfaces;
using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;

namespace RuneRogue.Core
{
    public class Actor : IActor, IDrawable, IScheduleable
    {
        // IActor
        private int _attack;
        private int _attackChance;
        private int _attackSkill;
        private int _awareness;
        private int _defense;
        private int _defenseChance;
        private int _defenseSkill;
        protected int _gold;
        private int _health;
        private int _maxHealth;
        private string _name;
        private int _speed;
        private bool _lifedrainOnHit;
        private bool _lifedrainOnDamage;
        private bool _regeneration;
        private bool _vampiric;
        private bool _doppelganger;
        private bool _highImpactAttack;
        private bool _senseThoughts;
        private bool _undead;
        private bool _invisible;



        public int Attack
        {
            get
            {
                return _attack;
            }
            set
            {
                _attack = value;
            }
        }

        public int AttackChance
        {
            get
            {
                return _attackChance;
            }
            set
            {
                _attackChance = value;
            }
        }

        public int AttackSkill
        {
            get
            {
                return _attackSkill;
            }
            set
            {
                _attackSkill = value;
            }
        }

        public int Awareness
        {
            get
            {
                return _awareness;
            }
            set
            {
                _awareness = value;
            }
        }

        public int Defense
        {
            get
            {
                return _defense;
            }
            set
            {
                _defense = value;
            }
        }

        public int DefenseChance
        {
            get
            {
                return _defenseChance;
            }
            set
            {
                _defenseChance = value;
            }
        }

        public int DefenseSkill
        {
            get
            {
                return _defenseSkill;
            }
            set
            {
                _defenseSkill = value;
            }
        }

        public virtual int Gold
        {
            get
            {
                return _gold;
            }
            set
            {
                _gold = value;
            }
        }

        public int Health
        {
            get
            {
                return _health;
            }
            set
            {
                _health = value;
            }
        }

        public int MaxHealth
        {
            get
            {
                return _maxHealth;
            }
            set
            {
                _maxHealth = value;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        public int Speed
        {
            get
            {
                return _speed;
            }
            set
            {
                _speed = value;
            }
        }
        public bool IsInvisible
        {
            get { return _invisible; }
            set { _invisible = value; }
        }
        public bool IsUndead
        {
            get { return _undead; }
            set { _undead = value; }
        }
        public bool SALifedrainOnHit
        {
            get { return _lifedrainOnHit; }
            set { _lifedrainOnHit = value; }
        }
        public bool SALifedrainOnDamage
        {
            get { return _lifedrainOnDamage; }
            set { _lifedrainOnDamage = value; }
        }

        public bool SARegeneration
        {
            get { return _regeneration; }
            set { _regeneration = value; }
        }

        public bool SAVampiric
        {
            get { return _vampiric; }
            set { _vampiric = value; }
        }

        public bool SADoppelganger
        {
            get { return _doppelganger; }
            set { _doppelganger = value; }
        }

        // attack is more likely to overcome armor and also
        // does more damage
        public bool SAHighImpact
        {
            get { return _highImpactAttack; }
            set { _highImpactAttack = value; }
        }
        public bool SASenseThoughts
        {
            get { return _senseThoughts; }
            set { _senseThoughts = value; }
        }

        public void DoppelgangTransform()
        {
            // if @, already transformed
            if (Symbol == '@')
            {
                return;
            }
            Symbol = '@';
            MaxHealth = 3 + Game.Player.MaxHealth / 3;
            Health = MaxHealth;
            Attack = Convert.ToInt32((double)Game.Player.Attack * 0.9);
            Defense = Convert.ToInt32((double)Game.Player.Defense * 0.9);
            Gold = Game.Player.Gold / 2;
            AttackSkill = Game.Player.AttackSkill - 2;
            DefenseSkill = Game.Player.DefenseSkill - 2;
            Speed = Game.Player.Speed;
        }

        // IDrawable
        public RLColor Color { get; set; }
        public char Symbol { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public void Draw(RLConsole console, IMap map)
        {
            // ESP radius 16
            if(Game.Player.SASenseThoughts)
            {
                double dist = Math.Pow((Math.Pow((double)(Game.Player.X - X), 2.0) + Math.Pow((double)(Game.Player.X - X), 2.0)), 0.5);
                if (dist <= 16.0)
                {
                    console.Set(X, Y, Color, Colors.FloorBackgroundFov, Symbol);
                    return;
                }
            }

            // Don't draw actors in cells that haven't been explored
            if (!map.GetCell(X, Y).IsExplored)
            {
                return;
            }

            // Only draw the actor with the color and symbol when they are in field-of-view
            if (map.IsInFov(X, Y))
            {
                console.Set(X, Y, Color, Colors.FloorBackgroundFov, Symbol);
            }
            else
            {
                // When not in field-of-view just draw a normal floor
                console.Set(X, Y, Colors.Floor, Colors.FloorBackground, '.');
            }
        }

        // IScheduleable
        public int Time
        {
            get
            {
                return Speed;
            }
        }
    }
}
