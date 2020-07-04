using RogueSharpV3Tutorial.Core;
using RogueSharpV3Tutorial.Systems;

namespace RogueSharpV3Tutorial.Interfaces
{
   public interface IBehavior
   {
      bool Act( Monster monster, CommandSystem commandSystem );
   }
}