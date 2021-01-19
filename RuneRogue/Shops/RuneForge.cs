using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using RLNET;
using RogueSharp.DiceNotation;
using RuneRogue.Core;


namespace RuneRogue.Shops
{


    public class RuneForge : Shop
    {

        private int _runeCost;

        public RuneForge()
        {
            Symbol = 'F';

            _runeCost = RoundFive(55.0 * DungeonLevelFactor(Game.mapLevel));
            _choiceDescription = "This forge will let you work a rune, but you will have to pay for the orichalcum bars yourself.";

            UpdateInventory();

        }

        // update runes available
        public override void UpdateInventory()
        {
            List<string> runeList = Game.RuneSystem.RunesNotOwned();
            _goods = new List<string>();
            _costs = new List<int>();

            runeList.ForEach(r => _goods.Add("Forge a Rune of " + r));
            runeList.ForEach(r => _costs.Add(_runeCost));
            _targets = runeList;

        }

        public override void ReceivePurchase(int purchaseIndex)
        {
            Game.RuneSystem.AcquireRune(_targets[purchaseIndex]);
            _goods.RemoveAt(purchaseIndex);
            _costs.RemoveAt(purchaseIndex);
            _targets.RemoveAt(purchaseIndex);
            _numOptions -= 1;
        }
    }
}
