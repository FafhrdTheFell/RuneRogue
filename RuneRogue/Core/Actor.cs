using RLNET;
using RogueSharp;
using RogueSharp.DiceNotation;
using RuneRogue.Interfaces;
using RuneRogue.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;

namespace RuneRogue.Core
{
    public class Actor : IActor, IDrawable, IScheduleable
    {
        // IActor
        private int _attack;
        private int _weaponSkill;
        private int _missileAttack;
        private int _missileRange;
        private int _specialAttackRange;
        private string _specialAttackType;
        private string _missileType;
        private int _awareness;
        private int _armor;
        private int _dodgeSkill;
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
        private bool _immobile;
        private bool _stealthy;
        private bool _ferocious;
        private bool _venomous;
        private bool _causesStun;

        private List<Effect> _currentEffects = new List<Effect>();

        public List<Effect> CurrentEffects
        {
            get { return _currentEffects; }
        }

        public Effect ExistingEffect(string effectType)
        {
            return _currentEffects.FirstOrDefault(d => d.EffectType == effectType);
        }

        public void AddEffect(Effect effect)
        {
            _currentEffects.Add(effect);
        }

        // to remove an effect, call FinishEffect on the effect
        public void RemoveEffect(Effect effect, bool calledFromEffect = false)
        {
            if (!calledFromEffect)
            {
                effect.FinishEffect();
            }
            _currentEffects.Remove(effect);
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

        public int MissileAttack
        {
            get { return _missileAttack; }
            set { _missileAttack = value; }
        }

        public int MissileRange
        {
            get { return _missileRange; }
            set { _missileRange = value; }
        }
        public string MissileType
        {
            get { return _missileType; }
            set { _missileType = value; }
        }

        public int SpecialAttackRange
        {
            get { return _specialAttackRange; }
            set { _specialAttackRange = value; }
        }
        public string SpecialAttackType
        {
            get { return _specialAttackType; }
            set { _specialAttackType = value; }
        }


        public int WeaponSkill
        {
            get
            {
                return _weaponSkill;
            }
            set
            {
                _weaponSkill = value;
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

        public int Armor
        {
            get
            {
                return _armor;
            }
            set
            {
                _armor = value;
            }
        }

        public int DodgeSkill
        {
            get
            {
                return _dodgeSkill;
            }
            set
            {
                _dodgeSkill = value;
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
        public bool SAStealthy
        {
            get { return _stealthy; }
            set { _stealthy = value; }
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
        public bool IsPoisoned
        {
            get { return this.ExistingEffect("poison") != null; }
        }
        public bool IsImmobile
        {
            get { return _immobile; }
            set { _immobile = value; }
        }
        public bool SAFerocious
        {
            get { return _ferocious; }
            set { _ferocious = value; }
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
        public bool SAVenomous
        {
            get { return _venomous; }
            set { _venomous = value; }
        }
        public bool SACausesStun
        {
            get { return _causesStun; }
            set { _causesStun = value; }
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

        public bool NextTo(Actor actor)
        {
            return ((this.X - actor.X) * (this.X - actor.X) < 2 && (this.Y - actor.Y) * (this.Y - actor.Y) < 2);
        }

        public bool WithinDistance(Actor actor, int distance)
        {
            return ((this.X - actor.X) * (this.X - actor.X) + (this.Y - actor.Y) * (this.Y - actor.Y) <= 
                distance * distance + distance);
        }

        public bool WithinDistance(Cell cell, int distance)
        {
            return ((this.X - cell.X) * (this.X - cell.X) + (this.Y - cell.Y) * (this.Y - cell.Y) <=
                distance * distance + distance);
        }

        public void DoppelgangTransform()
        {
            // if @, already transformed
            Symbol = '@';
            MaxHealth = 3 + Game.Player.MaxHealth / 3;
            Health = MaxHealth;
            Attack = Convert.ToInt32((double)Game.Player.Attack * 0.9);
            Armor = Convert.ToInt32((double)Game.Player.Armor * 0.9);
            Gold = Game.Player.Gold / 2;
            WeaponSkill = Game.Player.WeaponSkill - 2;
            DodgeSkill = Game.Player.DodgeSkill - 2;
            Speed = Game.Player.Speed;
        }

        // IDrawable
        public RLColor Color { get; set; }
        public char Symbol { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public void Draw(RLConsole console, IMap map)
        {
            if(Game.Player.SASenseThoughts)
            {
                if (WithinDistance(Game.Player, RunesSystem.DistanceSenseThoughts))
                {
                    console.Set(X, Y, Color, Colors.FloorBackgroundFov, Symbol);
                    return;
                }
            }

            // Don't draw actors in cells that haven't been explored or aren't visible
            if (!map.GetCell(X, Y).IsExplored || !map.GetCell(X, Y).IsInFov)
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
                int turnSpeed = Speed;
                // Ferocious Actors have 50% chance of moving twice as quickly
                turnSpeed = (SAFerocious && Dice.Roll("1d2") == 1) ? (turnSpeed * 2) : turnSpeed;
                
                // round (100 / Speed) probabilistically so that expect Actor
                // with speed x to have an average of x turns per 100 clicks
                int turnClicks = 100 / turnSpeed;
                int remainder = 100 % turnSpeed;
                if (Dice.Roll("1d"+(Speed*100).ToString()) <= remainder * 100)
                {
                    turnClicks++;
                }

                return turnClicks;
            }
        }
    }
}
