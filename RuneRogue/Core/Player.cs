﻿using RLNET;
using System;
using RogueSharp;
using RogueSharp.DiceNotation;
using System.Collections.Generic;
using System.Linq;

namespace RuneRogue.Core
{
    public class Player : Actor
    {
        private int _xpAttackSkill;
        private int _xpDefenseSkill;
        private int _xpHealth;
        private int _xpTotalReceived;
        private int _lifetimeGold;
        private List<Monster> _monstersIgnore;

        public int StatLines = 26;

        public int LifetimeGold
        {
            get { return _lifetimeGold; }
            set { _lifetimeGold = value; }
        }

        public override int Gold
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

        public List<Monster> MonstersIgnoreList
        {
            get { return _monstersIgnore; }
            set { _monstersIgnore = value; }
        }

        public bool IgnoreMonsters(List<Monster> monsterList)
        {
            if (_monstersIgnore.Count < monsterList.Count)
            {
                return true;
            }
            return monsterList.Except(_monstersIgnore).Any();
        }

        // PlayerPeril is true if player can see monster
        public bool PlayerPeril;

        public int XpAttackSkill
        {
            get { return _xpAttackSkill; }
            set 
            {
                if (value > _xpAttackSkill)
                {
                    XpTotalReceived += value - _xpAttackSkill;
                }
                _xpAttackSkill = value; 
            }
        }

        public int XpDefenseSkill
        {
            get { return _xpDefenseSkill; }
            set 
            {
                if (value > _xpHealth)
                {
                    XpTotalReceived += value - _xpDefenseSkill;
                }
                _xpDefenseSkill = value; 
            }
        }

        public int XpHealth
        {
            get { return _xpHealth; }
            set
            {
                if (value > _xpHealth)
                {
                    XpTotalReceived += value - _xpHealth;
                }
                _xpHealth = value;
            }
        }

        public int XpTotalReceived
        {
            get { return _xpTotalReceived; }
            set { _xpTotalReceived = value; }
        }

        public Player()
        {
            Attack = 10;
            WeaponSkill = 5;
            Awareness = 15;
            Color = Colors.Player;
            Armor = 5;
            DodgeSkill = 5;
            Gold = 100;
            XpAttackSkill = 0;
            XpDefenseSkill = 0;
            XpHealth = 0;
            XpTotalReceived = 0;
            Health = 30;
            MaxHealth = 30;
            Name = "";
            Speed = 10;
            Symbol = '@';
            MonstersIgnoreList = new List<Monster>();
        }

        public void CheckAdvancementXP()
        {
            int factor = 8;
            int factorHealth = 2;
            if (XpAttackSkill >= WeaponSkill * factor)
            {
                CheckAdvancement("attack", 1);
            }
            if (XpDefenseSkill >= DodgeSkill * factor)
            {
                CheckAdvancement("defense", 1);
            }
            if (XpHealth >= MaxHealth * factorHealth)
            {
                CheckAdvancement("health", 1);
            }
        }

        public void CheckAdvancement(string skill, int checks)
        {
            for (int i = 0; i < checks; i++)
            {
                switch (skill)
                {
                    case "attack":
                        if (Dice.Roll("1d20") >= WeaponSkill)
                        {
                            WeaponSkill += 1;
                            Game.MessageLog.Add($"{Name} has learned more about attacking.");
                        }
                        break;
                    case "defense":
                        if (Dice.Roll("1d20") >= DodgeSkill)
                        {
                            DodgeSkill += 1;
                            Game.MessageLog.Add($"{Name} has learned more about defending.");
                        }
                        break;
                    case "health":
                        Health = MaxHealth; // always at least heal fully
                        if (Dice.Roll("1d60") >= MaxHealth)
                        {
                            MaxHealth += Dice.Roll("1d2");
                            Health = MaxHealth;
                            Game.MessageLog.Add($"{Name} has gotten tougher.");
                        }
                        break;
                    default:
                        throw new ArgumentException($"Invalid skill {skill}.");
                }
            }
        }

        public void DrawStats(RLConsole statConsole)
        {
            statConsole.Print(1,  1, $"Dungeon level: {Game.mapLevel}", Colors.Text);
            statConsole.Print(1,  3, $"Name:", Colors.Text);
            statConsole.Print(1,  5, $"    {Name}", Colors.Text);
            statConsole.Print(1,  7, $"Health:", Colors.Text);
            statConsole.Print(1, 11, $"Attack:", Colors.Text);
            statConsole.Print(1, 13, $"    {WeaponSkill} skill", Colors.Text);
            statConsole.Print(1, 14, $"    {Attack} weaponry", Colors.Text);
            statConsole.Print(1, 16, $"Armor:", Colors.Text);
            statConsole.Print(1, 18, $"    {DodgeSkill} skill", Colors.Text);
            statConsole.Print(1, 19, $"    {Armor} armor", Colors.Text);
            statConsole.Print(1, 21, $"Gold:", Colors.Gold);
            statConsole.Print(1, 23, $"    {Gold}", Colors.Gold);
            statConsole.Print(1, 24, $"    {LifetimeGold} (lifetime)", Colors.Gold);

            statConsole.Print(13, Game.MapHeight - 2, $"({X}, {Y})", Colors.TextInactive);

            // print health bar
            // Begin the line by printing the health numbers 
            int width;
            if (Health > 0)
            {
                statConsole.Print(1, 9, $"{Health}/{MaxHealth}", Colors.Text);
                // Figure out the width of the health bar by dividing current health by max health
                width = Convert.ToInt32(((double)Health / (double)MaxHealth) * 12.0);
                
            }
            else
            {
                statConsole.Print(1, 9, $"DEAD!", Swatch.DbBlood);
                width = 0;
            }
            
            int remainingWidth = 12 - width;
            // Set the background colors of the health bar to show how damaged the monster is
            RLColor healthBarColor = Swatch.PrimaryLighter;
            if (this.IsPoisoned)
            {
                healthBarColor = Swatch.DbGrass;
            }
            statConsole.SetBackColor(9, 7, width, 1, healthBarColor);
            statConsole.SetBackColor(9 + width, 7, remainingWidth, 1, Swatch.PrimaryDarkest);
        }
    }
}
