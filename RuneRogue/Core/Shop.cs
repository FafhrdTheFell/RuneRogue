using RLNET;
using RogueSharp;
using RuneRogue.Interfaces;
using System;
using System.Collections.Generic;

namespace RuneRogue.Core
{
    public class Shop : IDrawable
    {

        protected string _storeDescription;
        protected List<string> _goods;
        protected List<int> _costs;

        // for shop prices display:
        private int _verticalOffset = 4;
        private int _horizontalOffset = 4;

        public string StoreDescription
        {
            get { return _storeDescription; }
            set { _storeDescription = value; }
        }

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

        public Shop()
        {
            
            Symbol = 'M';
            Color = Colors.Door;
            BackgroundColor = Colors.Gold;

            _storeDescription = "";
            _goods = new List<string>();
            _costs = new List<int>();

            _goods.Add("Increase atttack!");
            _goods.Add("No.");
            _goods.Add("longer still");

            _costs.Add(0);
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

        public virtual void UpdateCosts()
        {

        }

        // return false if still shopping, true if finished
        public virtual bool PurchaseChoice(RLKeyPress rLKeyPress)
        {
            int[] costs = _costs.ToArray();
            if (rLKeyPress.Char == null)
            {
                return false;
            }
            if (rLKeyPress.Key == RLKey.X || rLKeyPress.Key == RLKey.Escape)
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

        public virtual void ReceivePurchase(int purchase)
        {
            switch (purchase)
            {
                case 1:
                    Game.Player.Attack += 1;
                    Game.MessageLog.Add(Game.Player.Name + " upgrades their weaponry.");
                    break;
                default:
                    break;
            }
        }

        public bool HasDescription()
        {
            return (!(StoreDescription == ""));
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
        public void DrawConsole(RLConsole console)
        {
            int displayNumber;
            string nameOfGood;
            string costString;

            UpdateCosts();

            int descriptionOffset = 0;
            if (HasDescription())
            {
                console.Print(_horizontalOffset, _verticalOffset, StoreDescription, Colors.TextHeading);
                descriptionOffset += 4;
            }
            for (int i = 0; i < _goods.Count; i++)
            {
                displayNumber = i + 1;
                nameOfGood = Goods[i];
                // trailing spaces so when costs drop from 10 to 1, the 0 gets overwritten
                costString = Costs[i].ToString() + "   ";
                console.Print(_horizontalOffset, descriptionOffset + _verticalOffset + 2 * i, 
                    "(" + displayNumber.ToString() + ")", Colors.Text);
                console.Print(_horizontalOffset + 4, descriptionOffset + _verticalOffset + 2 * i, nameOfGood, Colors.Text);
                console.Print(_horizontalOffset + 64, descriptionOffset + _verticalOffset + 2 * i, costString, Colors.Text);
            }
            console.Print(_horizontalOffset, descriptionOffset + 1 + _verticalOffset + 2 * _goods.Count,
                "( X ) Exit shop.", Colors.Text);
        }
    }
}
