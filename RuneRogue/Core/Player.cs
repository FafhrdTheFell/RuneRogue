﻿using RLNET;

namespace RuneRogue.Core
{
   public class Player : Actor
   {
      public Player()
      {
         Attack = 2;
         AttackChance = 50;
         Awareness = 15;
         Color = Colors.Player;
         Defense = 2;
         DefenseChance = 40;
         Gold = 100;
         Health = 25;
         MaxHealth = 25;
         Name = "Rogue";
         Speed = 10;
         Symbol = '@';
      }

      public void DrawStats( RLConsole statConsole )
      {
         statConsole.Print( 1, 1, $"Name:    {Name}", Colors.Text );
         statConsole.Print( 1, 3, $"Health:  {Health}/{MaxHealth}", Colors.Text );
         statConsole.Print( 1, 5, $"Attack:  {Attack} ({AttackChance}%)", Colors.Text );
         statConsole.Print( 1, 7, $"Defense: {Defense} ({DefenseChance}%)", Colors.Text );
         statConsole.Print( 1, 9, $"Gold:    {Gold}", Colors.Gold );
      }
   }
}
