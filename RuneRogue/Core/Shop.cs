using RLNET;
using RogueSharp;
using RuneRogue.Interfaces;
using System;
using System.Collections.Generic;

namespace RuneRogue.Core
{
   public class Shop : IDrawable
   {

        protected  List<string> _goods;
        protected  List<int> _costs;

        // for shop prices display:
        private int _verticalOffset = 4;
        private int _horizontalOffset = 4;

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

        _goods = new List<string>();
        _costs = new List<int>();

        _goods.Add("Increase atttack!");
        _goods.Add("No.");
        _goods.Add("longer still");

        _costs.Add(10);
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

        // Draw is draw function for drawing Shop sprite / letter in
        // map console
        public void Draw( RLConsole console, IMap map )
      {
         if ( !map.GetCell( X, Y ).IsExplored )
         {
            return;
         }

         if ( map.IsInFov( X, Y ) )
         {
            Color = Colors.DoorFov;
            BackgroundColor = Colors.DoorBackgroundFov;
         }
         else
         {
            Color = Colors.Door;
            BackgroundColor = Colors.DoorBackground;
         }

         console.Set( X, Y, Color, BackgroundColor, Symbol );
      }

        // DrawConsole draws secondary console displaying shop
        // items and prices
        public void DrawConsole(RLConsole console)
        {
            UpdateCosts();
            //string[] lines = _goods.ToArray();
            //int displayNumber;
            //for (int i = 0; i < lines.Length; i++)
            //{
            //    displayNumber = i + 1;
            //    console.Print(_horizontalOffset, _verticalOffset + 2 * i, "(" + displayNumber.ToString() + ") " + lines[i], Colors.Text);
            //}
            int displayNumber;
            string nameOfGood;
            string costString;
            for (int i = 0; i < _goods.Count; i++)
            {
                displayNumber = i + 1;
                nameOfGood = Goods[i];
                costString = Costs[i].ToString();
                console.Print(_horizontalOffset, _verticalOffset + 2 * i, "(" + displayNumber.ToString() + ")", Colors.Text);
                console.Print(_horizontalOffset + 4, _verticalOffset + 2 * i, nameOfGood, Colors.Text);
                console.Print(_horizontalOffset + 64, _verticalOffset + 2 * i, costString, Colors.Text);
            }
            console.Print(_horizontalOffset, 1 + _verticalOffset + 2 * _goods.Count, "( X ) Exit shop.", Colors.Text);
        }
    }
}
