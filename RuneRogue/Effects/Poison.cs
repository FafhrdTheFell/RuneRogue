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
        int _activationDamage;

        public int ActivationDamage
        {
            get { return _activationDamage; }
            set { _activationDamage = value; }
        }

        // if totalDamage is not evenly divisible by activationDamage, round up to a
        // multiple of activationDamage. Then
        // duration is = # activations * speed = (totalDamage / activationDamage) * speed
        // e.g. 35 total, 8 act, 6 speed => 30 => 5 activations, 8 damage each
        public Poison(Actor poisoned, int totalDamage, int speed, int activationDamage) 
            : base(poisoned, speed, 
                  speed * (totalDamage / activationDamage + 1 * Convert.ToInt32(totalDamage % activationDamage > 0)))
        {
            ActivationDamage = activationDamage; 
        }

        public override void PerformEffectOn(Actor target)
        {
            Player player = Game.Player;
            target.Health -= ActivationDamage;
            if (target == Game.Player)
            {
                Game.MessageLog.Add($"{target.Name} takes {ActivationDamage} poison damage.");
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
