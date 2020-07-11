using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using RLNET;
using RuneRogue.Core;

// this class handles the interface of shops that Player
// visits

namespace RuneRogue.Core
{
    //public class Ref<T> where T : struct
    //{ 
    //    public T Value { get; set; }
    //}

    //public class ValueWrapper<T>
    //where T : struct
    //{
    //    public ValueWrapper(T item)
    //    {
    //        this.Item = item;
    //    }

    //    public T Item { get; set; }
    //}

    public class TrainingShop : Shop
    {

        protected List<string> _target;

        public List<string> Target
        { 
            get { return _target; }
            set { _target = value;  }
        }

        public TrainingShop()
        {
            _goods = new List<string>();
            _costs = new List<int>();
            _target = new List<string>();

            _goods.Add("Increase attack.");
            _goods.Add("Increase defense.");
            _goods.Add("Eat food (restores health).");

            _costs.Add(10);
            _costs.Add(10);
            _costs.Add(10);

            _target.Add("Attack");
            _target.Add("Defense");
            _target.Add("Health");

        }

        private int RoundFive(double number)
        {
            double rem = (number + 2.5) % 5;
            return Convert.ToInt32(number + 2.5 - rem);
        }

        public override void UpdateCosts()
        {
            Costs[0] = RoundFive(1.5 * Math.Pow(Convert.ToDouble(Game.Player.Attack + 1), 1.35));
            Costs[1] = RoundFive(3.0 * Math.Pow(Convert.ToDouble(Game.Player.Defense + 1), 1.65));
            if (Game.Player.Health == Game.Player.MaxHealth)
            {
                Costs[2] = 10;
            }
            else
            {
                Costs[2] = 1;
            }

        }


        // return false if still shopping, true if finished
        public override bool PurchaseChoice(RLKeyPress rLKeyPress)
        {
            UpdateCosts();
            int[] costs = _costs.ToArray();
            if (rLKeyPress.Char == null)
            {
                return false;
            }
            if (rLKeyPress.Key == RLKey.X)
            {
                return true;
            }
            //int choice = System.Char.GetNumericValue((Char)rLKeyPress.Char);
            int purchase = int.Parse(rLKeyPress.Char.ToString());
            if (purchase == -1)
            {
                return false;
            }
            // menu starts at 1
            if (Game.Player.Gold >= costs[purchase - 1])
            {
                Game.Player.Gold -= costs[purchase - 1];
                ReceivePurchase(purchase);
                return false;
            }
            else
            {
                Game.MessageLog.Add(Game.Player.Name + " cannot afford that.");
                return false;
            }
        }

        public override void ReceivePurchase(int purchase)
        {
            switch (_target[purchase-1])
            {
                case "Attack":
                    Game.Player.Attack += 1;
                    Game.MessageLog.Add(Game.Player.Name + " upgrades their weaponry.");
                    break;
                case "Defense":
                    Game.Player.Defense += 1;
                    Game.MessageLog.Add(Game.Player.Name + " upgrades their armor.");
                    break;
                case "Health":
                    if (Game.Player.Health == Game.Player.MaxHealth)
                    {
                        Game.Player.MaxHealth += 1;
                        Game.Player.Health += 1;
                        Game.MessageLog.Add(Game.Player.Name + " feasts and grows stronger.");
                    }
                    else
                    {
                        Game.Player.Health += 1;
                        Game.MessageLog.Add(Game.Player.Name + " rests and recuperates.");
                    }
                    break;
            }

        }
    }
}
