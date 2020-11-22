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
        int _starttime;
        int _duration;

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

        public int Duration
        {
            get { return _duration; }
            set { _duration = value; }
        }

        public virtual int Time
        {
            get { return Speed; }
        }

        public Effect(Actor effected)
        {
            _target = effected;
            _starttime = Game.SchedulingSystem.GetTime();
            StartEffect();
        }

        public virtual void StartEffect()
        {

        }

        public virtual void FinishEffect()
        {

        }


        public virtual void DoEffect()
        {
            if (_starttime + _duration >= Game.SchedulingSystem.GetTime())
            {
                FinishEffect();
            }
        }

        public virtual void PerformEffectOn(Actor actor)
        {

        }

        public virtual bool EffectFinished()
        {
            return _starttime + _duration >= Game.SchedulingSystem.GetTime();
        }


    }

}
