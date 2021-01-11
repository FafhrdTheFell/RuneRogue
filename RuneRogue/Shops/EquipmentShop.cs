using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using RLNET;
using RuneRogue.Core;

// this class handles the interface of shops that Player
// visits

namespace RuneRogue.Shops
{

    public class EquipmentShop : Shop
    {

        public EquipmentShop()
        {
            _choiceDescription = "This market has stalls with various weapons and armor, as well as food.";
            _goods = new List<string>();
            _costs = new List<int>();
            _targets = new List<string>();

            _goods.Add("Increase attack.");
            _goods.Add("Increase defense.");
            _goods.Add("Eat food (restores health).");

            _costs.Add(10);
            _costs.Add(10);
            _costs.Add(10);

            _targets.Add("Attack");
            _targets.Add("Armor");
            _targets.Add("Health");

            UpdateInventory();
        }

        // updates prices
        public override void UpdateInventory()
        {
            double newAttack = Convert.ToDouble(Game.Player.Attack + 1);
            double newDefense = Convert.ToDouble(Game.Player.Armor + 1);
            Costs[0] = RoundFive(1.5 * Math.Pow(newAttack, 1.35) * DungeonLevelFactor(Game.mapLevel));
            Costs[1] = RoundFive(2.0 * Math.Pow(newDefense, 1.55) * DungeonLevelFactor(Game.mapLevel));
            if (Game.Player.Health == Game.Player.MaxHealth)
            {
                Costs[2] = Convert.ToInt32(8.0 * DungeonLevelFactor(Game.mapLevel));
            }
            else
            {
                Costs[2] = Convert.ToInt32(DungeonLevelFactor(Game.mapLevel));
            }

        }


        public override void ReceivePurchase(int purchaseIndex)
        {
            switch (_targets[purchaseIndex])
            {
                case "Attack":
                    Game.Player.Attack += 1;
                    Game.MessageLog.Add(Game.Player.Name + " upgrades their weaponry.");
                    break;
                case "Armor":
                    Game.Player.Armor += 1;
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
