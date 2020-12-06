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
        int _potency;

        public int ActivationDamage
        {
            get { return _potency; }
            //set { _activationDamage = value; }
            //get { return _activationDamage; }
            //set { _activationDamage = value; }
        }

        public int Potency
        {
            get { return _potency; }
            set { _potency = value; }
        }

        // if totalDamage is not evenly divisible by activationDamage, round up to a
        // multiple of activationDamage. Then
        // duration is = # activations * speed = (totalDamage / activationDamage) * speed
        // e.g. 35 total, 8 act, 6 speed => 30 => 5 activations, 8 damage each
        // potency
        // speed 40 / p
        // totaldamage p^2+2*p 3,8,15,24,
        // act damage p
        // # of acts = p+2
        // duration = (p+2)*40/p = 40+80/p
        public Poison(Actor poisoned, int potency)
            : base(poisoned, 40 / potency, 40 + 80 / potency)
        //public Poison(Actor poisoned, int totalDamage, int speed, int activationDamage) 
        //    : base(poisoned, speed, 
        //          speed * (totalDamage / activationDamage + 1 * Convert.ToInt32(totalDamage % activationDamage > 0)))
        {
            //ActivationDamage = activationDamage;
            Potency = potency;
            //ActivationDamage = potency;
            EffectType = "poison";
        }
        public override void StartEffect()
        {
            if (Target.ExistingEffect("poison") != null)
            {
                Poison oldPoison = Target.ExistingEffect("poison") as Poison;
                int newPotency = Math.Max(oldPoison.Potency, this.Potency) + 1;
                //int totalDamage = ActivationDamage * Duration / Speed;
                int timeLeft = oldPoison.Duration - (Game.SchedulingSystem.GetTime() - oldPoison.StartTime);
                int newDuration = timeLeft + Duration;
                //int newActivationDamage = (ActivationDamage + oldPoison.ActivationDamage) / 2 + 1;
                oldPoison.Duration = newDuration;
                oldPoison.Potency = newPotency;
                //oldPoison.ActivationDamage = newActivationDamage;
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
