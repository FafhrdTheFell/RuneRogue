using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using RLNET;
using RogueSharp.DiceNotation;
using RuneRogue.Core;


namespace RuneRogue.Shops
{


    public class BookShop : Shop
    {

        protected List<string> _target;
        private bool _bookPurchased;

        public List<string> Target
        { 
            get { return _target; }
            set { _target = value;  }
        }

        public bool BookPurchasd
        {
            get { return _bookPurchased; }
            set { _bookPurchased = value; }
        }

        public BookShop()
        {
            Symbol = 'B';
            _goods = new List<string>();
            _costs = new List<int>();
            _target = new List<string>();

            _storeDescription = "This bookstore sells instruction manuals. LIMIT ONE BOOK PER CUSTOMER.";
            _goods.Add("Increase attack skill.");
            _goods.Add("Increase defense skill.");
            _goods.Add("Learn to be tougher.");

            int levelBookCost = 25 + 25 * (Game.mapLevel / 4);
            _costs.Add(levelBookCost);
            _costs.Add(levelBookCost);
            _costs.Add(levelBookCost);

            _target.Add("Attack Skill");
            _target.Add("Defense Skill");
            _target.Add("Health");

        }


        // return false if still shopping, true if finished
        public override bool PurchaseChoice(RLKeyPress rLKeyPress)
        {
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
                case "Attack Skill":
                    Game.Player.AttackSkill += Dice.Roll("1d2+1");
                    Game.MessageLog.Add(Game.Player.Name + " learns to attack aggressively.");
                    break;
                case "Defense Skill":
                    Game.Player.DefenseSkill += Dice.Roll("1d2+1");
                    Game.MessageLog.Add(Game.Player.Name + " learns to dodge and block.");
                    break;
                case "Health":
                    Game.Player.MaxHealth += Dice.Roll("2d3");
                    Game.Player.Health = Game.Player.MaxHealth;
                    Game.MessageLog.Add(Game.Player.Name + " toughens up.");
                    break;
            }
            Goods = new List<string>();
            Costs = new List<int>();
            BookPurchasd = true;
        }
    }
}
