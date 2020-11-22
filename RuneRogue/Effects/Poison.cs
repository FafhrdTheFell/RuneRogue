using RuneRogue.Core;
using RuneRogue.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuneRogue.Effects
{
    class Poison : Effect
    {
        // damage each activation
        int _magnitude;

        public int Magnitude
        {
            get { return _magnitude; }
            set { _magnitude = value; }
        }

        public Poison(Actor poisoned, int totalDamage, int speed, int activationDamage) : base(poisoned)
        {
            Magnitude = activationDamage; 
            Speed = speed;
            Duration = totalDamage / speed; // # of activations
        }

        public override void PerformEffectOn(Actor target)
        {
            Player player = Game.Player;
            target.Health -= Magnitude;
            if (target == Game.Player)
            {
                Game.MessageLog.Add($"{target.Name} takes {Magnitude} poison damage.");
            }
            else
            {
                Game.DungeonMap.ComputeFov(player.X, player.Y, player.Awareness, true);
                if (target.Health > 0 && Game.DungeonMap.IsInFov(target.X, target.Y))
                {
                    Game.MessageLog.Add($"{target.Name} looks ill.");
                }
                else
                {
                    StringBuilder attackMessage = new StringBuilder();
                    attackMessage.AppendFormat("{0} dies from poisoning.", target.Name);
                    CommandSystem.ResolveDeath(target, attackMessage);
                    if (!string.IsNullOrWhiteSpace(attackMessage.ToString()) && Game.DungeonMap.IsInFov(target.X, target.Y))
                    {
                        Game.MessageLog.Add(attackMessage.ToString());
                    }
                    FinishEffect();
                }
            }

        }
    }
}
