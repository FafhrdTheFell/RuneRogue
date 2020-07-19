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
                string[] split = target.Split(new Char[] { ' ', ',', '.', ':', '\t' });
                _targets.Add(split[0]);
                _costs.Add(levelBookCost);
            }

            _numOptions = numBooks;

        }

        public override void ReceivePurchase(int purchaseIndex)
        {
            Game.Player.CheckAdvancement(_targets[purchaseIndex], 3);
            _goods.RemoveAt(purchaseIndex);
            _costs.RemoveAt(purchaseIndex);
            _targets.RemoveAt(purchaseIndex);
            _numOptions = _goods.Count;
        }
    }
}
