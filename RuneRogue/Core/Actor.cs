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
        private int _gold;
        private int _health;
        private int _maxHealth;
        private string _name;
        private int _speed;
        private int _lifetimeGold;

        public int LifetimeGold
        {
            get { return _lifetimeGold; }
            set { _lifetimeGold = value; }
        }

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

        public int Gold
        {
            get
            {
                return _gold;
            }
            set
            {
                int increase = Math.Max(value - _gold, 0);
                LifetimeGold += increase;
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

        // IDrawable
        public RLColor Color { get; set; }
        public char Symbol { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public void Draw(RLConsole console, IMap map)
        {
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
