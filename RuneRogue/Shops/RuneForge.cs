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
            _goods = new List<string>();
            _costs = new List<int>();
            _targets = new List<string>();

            _runeCost = RoundFive(40.0 * DungeonLevelFactor(Game.mapLevel));
            _storeDescription = "This forge will let you work a rune, but you will have to pay for the orichalcum bars yourself.";

            UpdateInventory();

        }

        public override void UpdateInventory()
        {
            List<string> runeList = Game.RuneSystem.RunesNotOwned();
            _goods = new List<string>();
            _costs = new List<int>();
            _targets = new List<string>();

            for (int i = 0; i < runeList.Count; i++)
            {
                _goods.Add("Forge a Rune of " + runeList[i]);
                _costs.Add(_runeCost);
                _targets.Add(runeList[i]);
            }

            _numOptions = runeList.Count;
        }

        public override void ReceivePurchase(int purchaseIndex)
        {
            Game.RuneSystem.AddRuneAbility(_targets[purchaseIndex]);
            _goods.RemoveAt(purchaseIndex);
            _costs.RemoveAt(purchaseIndex);
            _targets.RemoveAt(purchaseIndex);
            _numOptions -= 1;
        }
    }
}
