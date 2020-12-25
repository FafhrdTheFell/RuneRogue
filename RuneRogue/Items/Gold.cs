using RuneRogue.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuneRogue.Items
{
    class Gold : Item
    {
        int _amount;

        public int Amount
        {
            get { return _amount; }
            set { _amount = value; }
        }

        public Gold(int amount)
        {
            _amount = amount;
            Symbol = '$';
            Color = Colors.Gold;
        }

        public override bool Pickup(Actor actor)
        {
            actor.Gold += Amount;
            if (actor == Game.Player)
            {
                Game.MessageLog.Add($"{actor.Name} picks up {Amount} gold.");
            }
            return true;
        }
    }
}
