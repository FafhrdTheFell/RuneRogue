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
        // damage each activation plus how long poison lasts
        private int _potency;

        public int ActivationDamage
        {
            get { return _potency; }
        }

        public int Potency
        {
            get { return _potency; }
            set { _potency = value; }
        }

        // duration = (p+2)*40/p = 40+80/p
        public Poison(Actor poisoned, int potency)
            : base(poisoned, 40 / potency, 40 + 80 / potency)
        {
            Potency = potency;
            EffectType = "poison";
        }
        public override void StartEffect()
        {
            // multiple instances of poison combine
            if (Target.ExistingEffect("poison") != null)
            {
                Poison oldPoison = Target.ExistingEffect("poison") as Poison;
                int newPotency = Math.Max(oldPoison.Potency, this.Potency) + 1;
                int timeLeft = oldPoison.Duration - (Game.SchedulingSystem.GetTime() - oldPoison.StartTime);
                int newDuration = timeLeft + Duration;
                oldPoison.Duration = newDuration;
                oldPoison.Potency = newPotency;
            }
            else
            {
                Game.SchedulingSystem.Add(this);
                Target.AddEffect(this);
            }
        }


        public override void PerformEffectOn(Actor target)
        {
            Player player = Game.Player;
            StringBuilder attackMessage = new StringBuilder(); 
            //target.Health -= ActivationDamage;

            Game.DungeonMap.ComputeFov(player.X, player.Y, player.Awareness, true);
            bool isVisible = Game.DungeonMap.IsInFov(target.X, target.Y);
            if (target is Player)
            {
                attackMessage.AppendFormat("{0} feels ill. ", target.Name);
            }
            else if (isVisible)
            {
                attackMessage.AppendFormat("{0} looks ill. ", target.Name);
            }
            CommandSystem.ResolveDamage("poison", target, ActivationDamage, false, attackMessage);
            if (target.Health <= 0)
            {
                FinishEffect();
            }
            if (!string.IsNullOrWhiteSpace(attackMessage.ToString()) && isVisible)
            {
                Game.MessageLog.Add(attackMessage.ToString());
            }


        }
    }
}
