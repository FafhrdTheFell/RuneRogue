using RuneRogue.Core;
using RuneRogue.Systems;

namespace RuneRogue.Interfaces
{
   public interface IBehavior
   {
      bool Act( Monster monster, CommandSystem commandSystem );
   }
}