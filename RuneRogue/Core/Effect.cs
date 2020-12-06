using RLNET;
using RogueSharp;
using RuneRogue.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuneRogue.Core
{
    public class Effect : IScheduleable
    {
        Actor _target;
        int _speed;
        int _starttime;
        int _duration;
        string _effectType;

        public Actor Target
        {
            get { return _target; }
            set { _target = value; }
        }

        public int StartTime
        {
            get { return _starttime; }
            set { _starttime = value; }
        }

        public int Speed
        {
            get { return _speed; }
            set { _speed = value; }
        }

        // number of clock ticks until effect stops
        public int Duration
        {
            get { return _duration; }
            set { _duration = value; }
        }

        public string EffectType
        {
            get { return _effectType; }
            set { _effectType = value; }
        }

        public virtual int Time
        {
            get { return Speed; }
        }

        public Effect(Actor effected, int speed, int duration)
        {
            _target = effected;
            Speed = speed;
            Duration = duration;
            _starttime = Game.SchedulingSystem.GetTime();
            StartEffect();
        }

        public virtual void StartEffect()
        {
            Game.SchedulingSystem.Add(this);
            Target.AddEffect(this);
        }

        public virtual void FinishEffect()
        {
            Game.SchedulingSystem.Remove(this);
            Target.RemoveEffect(this, calledFromEffect: true);
        }


        public virtual void DoEffect()
        {
            PerformEffectOn(Target);
            if (Game.SchedulingSystem.GetTime() - StartTime >= Duration)
            {
                FinishEffect();
            }
        }

        public virtual void PerformEffectOn(Actor actor)
        {

        }


    }

}
