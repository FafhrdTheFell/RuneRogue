using RuneRogue.Core;
using RuneRogue.Systems;

namespace RuneRogue.Interfaces
{
    public interface IItem
    {
        bool Pickup(Actor actor);
    }
}