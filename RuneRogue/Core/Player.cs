using RLNET;
using System;

namespace RuneRogue.Core
{
    public class Player : Actor
    {




        public Player()
        {
            Attack = 10;
            AttackChance = 50;
            AttackSkill = 5;
            Awareness = 15;
            Color = Colors.Player;
            Defense = 3;
            DefenseChance = 40;
            DefenseSkill = 4;
            Gold = 100;
            Health = 25;
            MaxHealth = 25;
            Name = "Rogue";
            Speed = 10;
            Symbol = '@';
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
            statConsole.Print(1, 22, $"    {Gold} (lifetime)", Colors.Gold);

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
