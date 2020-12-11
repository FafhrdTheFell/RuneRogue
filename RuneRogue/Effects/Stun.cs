using RuneRogue.Core;
using RuneRogue.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuneRogue.Effects
{
    class Stun : Effect
    {
        // damage each activation plus how long poison lasts
        private int _potency;

        public int Potency
        {
            get { return _potency; }
            set { _potency = value; }
        }

        public Stun(Actor poisoned, int potency)
            : base(poisoned, poisoned.Speed, 12 * potency)
        {
            Potency = potency;
            EffectType = "stun";
        }
        public override void StartEffect()
        {
            // multiple instances of poison combine
            if (Target.ExistingEffect("stun") != null)
            {
                Stun oldStun = Target.ExistingEffect("stun") as Stun;
                int newPotency = Math.Max(oldStun.Potency, this.Potency) + 1;
                int timeLeft = oldStun.Duration - (Game.SchedulingSystem.GetTime() - oldStun.StartTime);
                int newDuration = Duration - timeLeft;
                oldStun.Duration = newDuration;
                oldStun.Potency = newPotency;
                //Game.MessageLog.Add($"{Target.Name} is more stunned.");
            }
            else
            {
                //Game.MessageLog.Add($"{Target.Name} is stunned.");
                Game.SchedulingSystem.Add(this);
                Target.AddEffect(this);
                Game.SchedulingSystem.Remove(Target);
            }
        }

        public override void FinishEffect()
        {
            base.FinishEffect();
            Game.SchedulingSystem.Add(Target);
        }


        public override void PerformEffectOn(Actor target)
        {
            if (target.Health > 0)
            {
                Game.MessageLog.Add($"{target.Name} is dazed and cannot act.");
            }
            else if (target is Player)
            {
                FinishEffect();
            }
        }
    }
}
