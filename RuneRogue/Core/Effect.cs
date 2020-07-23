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
    class Effect : IScheduleable
    {
        Actor _target;
        int _speed;
        int _duration;
        int _timesactivated;

        public Actor Target
        {
            get { return _target; }
            set { _target = value; }
        }

        public int Time
        {
            get { return _speed; }
        }

        public int Speed
        {
            get { return _speed; }
            set { _speed = value; }
        }
        
        public int Duration
        {
            get { return _duration; }
            set { _duration = value; }
        }

        public int TimesActivated
        {
            get { return _timesactivated; }
            set { _timesactivated = value; }
        }

        public Effect()
        {
            _timesactivated = 0;
        }

        public virtual void PerformEffectOn(Actor target)
        {

        }

        public virtual void DoEffect()
        {
            TimesActivated++;
            PerformEffectOn(_target);

        }

        public virtual void FinishEffect()
        {
            TimesActivated = Duration;
            Game.SchedulingSystem.Remove(this);
        }

        public virtual bool EffectFinished()
        {
            return (TimesActivated >= Duration);
        }
    }
}
