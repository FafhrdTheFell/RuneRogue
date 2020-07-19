using RLNET;
using RogueSharp;
using RuneRogue.Interfaces;
using System;
using System.Collections.Generic;

namespace RuneRogue.Core
{
    public class Shop : SecondaryConsole, IDrawable 
    {

        protected string _storeDescription;
        protected List<string> _goods;
        protected List<int> _costs;
        protected List<string> _targets;

        private RLConsole _shopConsole;

        // for shop prices display:
        private readonly int _verticalOffset = 4;
        private readonly int _horizontalOffset = 4;

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

            _shopConsole = Console;

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

        public bool HasDescription()
        {
            return _storeDescription != "";
        }

        public virtual void UpdateInventory()
        {

        }

        // return false if still shopping, true if finished
        public override bool ProcessKeyInput(RLKeyPress rLKeyPress)
        {
            UpdateInventory();
            int[] costs = _costs.ToArray();
            if (rLKeyPress.Char == null)
            {
                return false;
            }
            if (rLKeyPress.Key == RLKey.X)
            {
                return true;
            }
            int purchase;
            bool isNumber = int.TryParse(rLKeyPress.Char.ToString(), out purchase);
            if (!isNumber)
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
        public override void DrawConsole()
        {
            int displayNumber;
            string nameOfGood;
            string costString;

            _shopConsole.Clear();
            _shopConsole.SetBackColor(0, 0, Game.MapWidth, Game.MapHeight, Swatch.Compliment);

            UpdateInventory();

            int descriptionOffset = 0;
            if (HasDescription())
            {
                _shopConsole.Print(_horizontalOffset, _verticalOffset, StoreDescription, Colors.TextHeading);
                descriptionOffset += 4;
            }
            for (int i = 0; i < _goods.Count; i++)
            {
                displayNumber = i + 1;
                nameOfGood = Goods[i];
                // trailing spaces so when costs drop from 10 to 1, the 0 gets overwritten
                costString = Costs[i].ToString() + "   ";
                _shopConsole.Print(_horizontalOffset, descriptionOffset + _verticalOffset + 2 * i, 
                    "(" + displayNumber.ToString() + ")", Colors.Text);
                _shopConsole.Print(_horizontalOffset + 4, descriptionOffset + _verticalOffset + 2 * i, nameOfGood, Colors.Text);
                _shopConsole.Print(_horizontalOffset + 72, descriptionOffset + _verticalOffset + 2 * i, costString, Colors.Text);
            }
            _shopConsole.Print(_horizontalOffset, descriptionOffset + 1 + _verticalOffset + 2 * _goods.Count,
                "( X ) Exit shop.", Colors.Text);
        }

    }
}
