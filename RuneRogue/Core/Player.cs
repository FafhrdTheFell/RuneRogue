using RLNET;
using System;
using RogueSharp;
using RogueSharp.DiceNotation;

namespace RuneRogue.Core
{
    public class Player : Actor
    {
        private int _xpAttackSkill;
        private int _xpDefenseSkill;
        private int _xpHealth;
        private int _xpTotalReceived;
        private int _lifetimeGold;

        //private Runes _runesystem;

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
            AttackChance = 50;
            AttackSkill = 5;
            Awareness = 15;
            Color = Colors.Player;
            Defense = 4;
            DefenseChance = 40;
            DefenseSkill = 4;
            Gold = 100;
            XpAttackSkill = 0;
            XpDefenseSkill = 0;
            XpHealth = 0;
            XpTotalReceived = 0;
            Health = 25;
            MaxHealth = 25;
            Name = "Rogue";
            Speed = 10;
            Symbol = '@';
        }

        public void CheckAdvancementXP()
        {
            int factor = 8;
            int factorHealth = 2;
            if (XpAttackSkill >= AttackSkill * factor)
            {
                CheckAdvancement("attack", 1);
            }
            if (XpDefenseSkill >= DefenseSkill * factor)
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
                        if (Dice.Roll("1d20") >= AttackSkill)
                        {
                            AttackSkill += 1;
                            Game.MessageLog.Add($"{Name} has learned more about attacking.");
                        }
                        break;
                    case "defense":
                        if (Dice.Roll("1d20") >= DefenseSkill)
                        {
                            DefenseSkill += 1;
                            Game.MessageLog.Add($"{Name} has learned more about defending.");
                        }
                        break;
                    case "health":
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
            statConsole.Print(1,  1, $"Name:", Colors.Text);
            statConsole.Print(1,  3, $"    {Name}", Colors.Text);
            statConsole.Print(1,  5, $"Health:", Colors.Text);
            statConsole.Print(1,  9, $"Attack:", Colors.Text);
            statConsole.Print(1, 11, $"    {AttackSkill} skill", Colors.Text);
            statConsole.Print(1, 12, $"    {Attack} weaponry", Colors.Text);
            statConsole.Print(1, 14, $"Defense:", Colors.Text);
            statConsole.Print(1, 16, $"    {DefenseSkill} skill", Colors.Text);
            statConsole.Print(1, 17, $"    {Defense} armor", Colors.Text);
            statConsole.Print(1, 19, $"Gold:", Colors.Gold);
            statConsole.Print(1, 21, $"    {Gold}", Colors.Gold);
            statConsole.Print(1, 22, $"    {LifetimeGold} (lifetime)", Colors.Gold);

            // print health bar
            // Begin the line by printing the health numbers 
            int width;
            if (Health > 0)
            {
                statConsole.Print(1, 7, $"{Health}/{MaxHealth}", Colors.Text);
                // Figure out the width of the health bar by dividing current health by max health
                width = Convert.ToInt32(((double)Health / (double)MaxHealth) * 12.0);
                
            }
            else
            {
                statConsole.Print(1, 7, $"DEAD!", Swatch.DbBlood);
                width = 0;
            }
            
            int remainingWidth = 12 - width;
            // Set the background colors of the health bar to show how damaged the monster is
            statConsole.SetBackColor(7, 7, width, 1, Swatch.PrimaryLighter);
            statConsole.SetBackColor(7 + width, 7, remainingWidth, 1, Swatch.PrimaryDarkest);
        }
    }
}
