using RogueSharp.DiceNotation;
using RuneRogue.Core;

namespace RuneRogue.Monsters
{
   public class BoneClaws : Monster
   {

        public static BoneClaws Create( int level )
      {
         int health = Dice.Roll( "1d3" );
         return new BoneClaws
         {
            Attack = Dice.Roll( "2D3" ) + level / 3,
            AttackChance = Dice.Roll( "25D4" ),
            Awareness = 10,
            Color = Colors.BoneClawsColor,
            Defense = Dice.Roll( "3D2" ) + level / 3,
            DefenseChance = Dice.Roll( "15D3" ),
            Gold = Dice.Roll( "5D5" ),
            Health = health,
            MaxHealth = health,
            Name = "Bone Claws",
            Speed = 12,
            Symbol = 'b',
            NumberAppearing = "5-2d4k1",
            MinLevel = 1,
            MaxLevel = 3
         };
        }
    }
}