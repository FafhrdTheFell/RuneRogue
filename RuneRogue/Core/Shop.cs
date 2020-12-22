using RLNET;
using RogueSharp;
using RuneRogue.Interfaces;
using System;
using System.Collections.Generic;

namespace RuneRogue.Core
{
    // a shop is a set of descriptions of goods, _goods,
    // a cost for each, _costs, and a set of _targets,
    // descriptions of what ReceivePurchase should do
    // for each good purchase
    public class Shop : ChoiceConsole, IDrawable 
    {

        protected List<string> _goods;
        protected List<int> _costs;
        protected List<string> _targets;

        //private RLConsole Console;


        public List<string> Goods
        {
            get { return _goods; }
            set { _goods = value; }
        }

        public List<int> Costs
        {
            get { return _costs; }
            set { _costs = value; }
        }
        public List<string> Targets
        {
            get { return _targets; }
            set { _targets = value; }
        }

        public Shop()
        {
            
            Symbol = 'M';
            Color = Colors.Door;
            BackgroundColor = Colors.Gold;

            _goods = new List<string>();
            _costs = new List<int>();

            _goods.Add("Increase attack!");
            _goods.Add("No.");
            _goods.Add("longer still");

            _costs.Add(0);
        }
        public override List<string> MenuOptions()
        {
            return Goods;
        }
        public override List<string> AdditionalDetails()
        {
            List<string> costString = new List<string>();
            Costs.ForEach(c => costString.Add(c.ToString()));
            return costString;
        }

        public override List<int> OptionsInactive()
        {
            return new List<int>();
        }
        public RLColor Color
        {
            get; set;
        }
        public RLColor BackgroundColor
        {
            get; set;
        }
        public char Symbol
        {
            get; set;
        }
        public int X
        {
            get; set;
        }
        public int Y
        {
            get; set;
        }

        // 1 at lvl 1, 5 at lvl 13
        public double DungeonLevelFactor(int level)
        {
            return 1.0 + 4.0 * ((double)level - 1.0) / 12.0;
        }

        public int RoundFive(double number)
        {
            double rem = (number + 2.5) % 5;
            return Convert.ToInt32(number + 2.5 - rem);
        }

        // updates available goods and prices
        public virtual void UpdateInventory()
        {

        }

        public override bool ProcessChoice(int choiceIndex)
        {
            UpdateInventory();
            //int[] costs = _costs.ToArray();
            if (Game.Player.Gold >= _costs[choiceIndex])
            {
                Game.Player.Gold -= _costs[choiceIndex];
                ReceivePurchase(choiceIndex);
                return false;
            }
            else
            {
                Game.MessageLog.Add(Game.Player.Name + " cannot afford that.");
                return false;
            }
        }

        public virtual void ReceivePurchase(int purchaseIndex)
        {
            switch (purchaseIndex)
            {
                case 0:
                    Game.Player.Attack += 1;
                    Game.MessageLog.Add(Game.Player.Name + " upgrades their weaponry.");
                    break;
                default:
                    break;
            }
        }

        // Draw is draw function for drawing Shop sprite / letter in
        // map console
        public void Draw(RLConsole console, IMap map)
        {
            if (!map.GetCell(X, Y).IsExplored)
            {
                return;
            }

            if (map.IsInFov(X, Y))
            {
                Color = Colors.ShopFov;
                BackgroundColor = Colors.DoorBackgroundFov;
            }
            else
            {
                Color = Colors.Shop;
                BackgroundColor = Colors.DoorBackground;
            }

            console.Set(X, Y, Color, BackgroundColor, Symbol);
        }

        // DrawConsole draws secondary console displaying shop
        // items and prices
        //public override void DrawConsole()
        //{
        //    int displayNumber;
        //    string nameOfGood;
        //    string costString;

        //    Console.Clear();
        //    Console.SetBackColor(0, 0, Game.MapWidth, Game.MapHeight, Swatch.Compliment);

        //    UpdateInventory();

        //    int descriptionOffset = 0;
        //    if (HasDescription())
        //    {
        //        Console.Print(_horizontalOffset, _verticalOffset, StoreDescription, Colors.TextHeading);
        //        descriptionOffset += 4;
        //    }
        //    for (int i = 0; i < _goods.Count; i++)
        //    {
        //        displayNumber = i + 1;
        //        nameOfGood = Goods[i];
        //        // trailing spaces so when costs drop from 10 to 1, the 0 gets overwritten
        //        costString = Costs[i].ToString() + "   ";
        //        Console.Print(_horizontalOffset, descriptionOffset + _verticalOffset + 2 * i, 
        //            "(" + displayNumber.ToString() + ")", Colors.Text);
        //        Console.Print(_horizontalOffset + 4, descriptionOffset + _verticalOffset + 2 * i, nameOfGood, Colors.Text);
        //        Console.Print(_horizontalOffset + 72, descriptionOffset + _verticalOffset + 2 * i, costString, Colors.Text);
        //    }
        //    Console.Print(_horizontalOffset, descriptionOffset + 1 + _verticalOffset + 2 * _goods.Count,
        //        "( X ) Exit shop.", Colors.Text);
        //}

    }
}
