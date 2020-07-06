using RogueSharp.DiceNotation;
using RuneRogue.Core;
using RuneRogue.Interfaces;

namespace RuneRogue.Monsters
{
    public class Dragon : Monster
    {

        public static Dragon Create( int level )
      {
         int health = Dice.Roll( "6D6" );
         return new Dragon {
            Attack = Dice.Roll( "5D2" ) + level / 3,
            AttackChance = Dice.Roll( "25D3+20" ),
            Awareness = 10,
            Color = Colors.DragonColor,
            Defense = Dice.Roll( "3d3k2" ) + level / 3,
            DefenseChance = Dice.Roll( "8D8" ),
            Gold = 0,
            Health = health,
            MaxHealth = health,
            Name = "Dragon",
            Speed = 14,
            Symbol = 'D',
            NumberAppearing = "1",
            MinLevel = 2,
            MaxLevel = 10
        };
      }
   }
}
