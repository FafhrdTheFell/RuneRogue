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

        protected readonly string[] bookSynonym =
            {
                "Manual of increasing ",
                "Treatise on increasing ",
                "Scribbled notes about ",
                "Encyclopedia of methods to increase ",
                "Pamphlet of secrets to increase ",
                "Crazy diagrams about increasing ",
                "Tattered scroll praising ",
                "Hand-written letters describing the benefits of "
            };

        protected readonly string[] targetOptions =
            {
                "attack skill.",
                "defense skill.",
                "health."
            };


        public BookShop()
        {
            Symbol = 'B';
            _goods = new List<string>();
            _costs = new List<int>();
            _targets = new List<string>();

            int numBooks = Dice.Roll("1+2d3k1");

            //int levelBookCost = 25 + 25 * (Game.mapLevel / 4);
            int levelBookCost = RoundFive(25.0 * DungeonLevelFactor(Game.mapLevel));
            _storeDescription = "This bookstore has a few how-to books for sale.";

            for (int i = 0; i < numBooks; i++)
            {

                string prefixString = (string)Game.RandomArrayValue(bookSynonym);
                string target = (string)Game.RandomArrayValue(targetOptions);
                _goods.Add(prefixString + target);
                _targets.Add(target);
                _costs.Add(levelBookCost);
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
