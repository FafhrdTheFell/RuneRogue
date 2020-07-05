using RogueSharp.DiceNotation;
using RuneRogue.Core;

namespace RuneRogue.Monsters
{
   public class Beetle : Monster
   {
      public static Beetle Create( int level )
      {
         int health = Dice.Roll( "2D3" );
         return new Beetle {
            Attack = Dice.Roll( "1D3" ) + level / 3,
            AttackChance = Dice.Roll( "25D3" ),
            Awareness = 10,
            Color = Colors.BeetleColor,
            Defense = Dice.Roll( "2D2" ) + level / 3,
            DefenseChance = Dice.Roll( "10D4" ),
            Gold = 0,
            Health = health,
            MaxHealth = health,
            Name = "Beetle",
            Speed = 7,
            Symbol = 'b'
         };
      }
   }
}
