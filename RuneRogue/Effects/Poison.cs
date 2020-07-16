using RuneRogue.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuneRogue.Effects
{
    class Poison : Effect
    {
        int _magnitude;

        public int Magnitude
        {
            get { return _magnitude; }
            set { _magnitude = value; }
        }


        public override void PerformEffectOn(Actor target)
        {
            target.Health -= Magnitude;
            if (target == Game.Player)
            {
                Game.MessageLog.Add($"{target.Name} takes {Magnitude} poison damage.");
            }
            else
            {
                Game.MessageLog.Add($"{target.Name} looks ill.");
            }

        }
    }
}
