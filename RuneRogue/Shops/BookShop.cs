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
        // _target indicates which attribute good i increases
        protected List<string> _targets;

        protected readonly string[] bookSynonym =
{
                "Manual of increasing ",
                "Treatise on increasing ",
                "Scribbled notes about ",
                "Encyclopedia of methods to increase ",
                "Pamphlet of secrets to increase ",
                "Crazy diagrams about increasing ",
                "Tattered scroll praising ",
                "Hand-written letters describing "
            };

        protected readonly string[] targetOptions =
        {
                "attack skill.",
                "defense skill.",
                "health."
            };

        public List<string> Targets
        { 
            get { return _targets; }
            set { _targets = value;  }
        }


        public BookShop()
        {
            Symbol = 'B';
            _goods = new List<string>();
            _costs = new List<int>();
            _targets = new List<string>();

            int numBooks = Dice.Roll("1+2d3k1");

            int levelBookCost = 25 + 25 * (Game.mapLevel / 4);
            _storeDescription = "This bookstore sells instruction manuals.";

            for (int i = 0; i < numBooks; i++)
            {

                string prefixString = (string)Game.RandomArrayValue(bookSynonym);
                string target = (string)Game.RandomArrayValue(targetOptions);
                _goods.Add(prefixString + target);
                _targets.Add(target);
                _costs.Add(levelBookCost);
            }

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
            int purchaseIndex = purchase - 1;
            if (Game.Player.Gold >= costs[purchaseIndex])
            {
                Game.Player.Gold -= costs[purchaseIndex];
                ReceivePurchase(purchaseIndex);
                return false;
            }
            else
            {
                Game.MessageLog.Add(Game.Player.Name + " cannot afford that.");
                return false;
            }
        }

        public override void ReceivePurchase(int purchaseIndex)
        {
            switch (_targets[purchaseIndex])
            {
                case "attack skill.":
                    Game.Player.AttackSkill += Dice.Roll("1d2+1");
                    Game.MessageLog.Add(Game.Player.Name + " learns to attack aggressively.");
                    break;
                case "defense skill.":
                    Game.Player.DefenseSkill += Dice.Roll("1d2+1");
                    Game.MessageLog.Add(Game.Player.Name + " learns to dodge and block.");
                    break;
                case "health.":
                    Game.Player.MaxHealth += Dice.Roll("2d3");
                    Game.Player.Health = Game.Player.MaxHealth;
                    Game.MessageLog.Add(Game.Player.Name + " toughens up.");
                    break;
            }
            _goods.RemoveAt(purchaseIndex);
            _costs.RemoveAt(purchaseIndex);
            _targets.RemoveAt(purchaseIndex);
        }
    }
}
