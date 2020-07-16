using RLNET;
using RogueSharp.DiceNotation;
using RuneRogue.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuneRogue.Items
{
    class Valuable : Item
    {
        int _value;
        string _description;

        protected readonly string[] valuableRandomKinds =
        {
                "Jeweled Chalice",
                "Ruby",
                "Diamond",
                "Sapphire",
                "Pearl Necklace",
                "Ornate Cloak",
                "Gold Statue"
        };

        public readonly char[] valuableRandomSymbol =
        {
            ';',
            '+',
            '+',
            '+',
            ':',
            '~',
            '8'
        };

        public readonly RLColor[] valuableRandomColor =
        {
            Colors.Gold,
            Swatch.DbBlood,
            Swatch.DbLight,
            Swatch.DbSky,
            Swatch.DbLight,
            Swatch.DwPurple,
            Colors.Gold
        };

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public int Amount
        {
            get { return _value; }
            set { _value = value; }
        }

        public void RandomKind()
        {
            // subtract 1 because index
            int kind = Dice.Roll("1d" + valuableRandomKinds.Length + " - 1");
            Description = valuableRandomKinds[kind];
            Symbol = valuableRandomSymbol[kind];
            Color = valuableRandomColor[kind];

        }

        public int RandomValue()
        {
            string diesize = (1 + 3 * Game.mapLevel).ToString();
            return Dice.Roll("10d" + diesize);

        }

        public Valuable()
        {
            _value = 0;
            RandomKind();
            Amount = RandomValue();
        }

        public override bool Pickup(Actor actor)
        {
            actor.Gold += Amount;
            if (actor == Game.Player)
            {
                Game.MessageLog.Add($"{actor.Name} picks up {Description}, worth {Amount} gold.");
            }
            return true;
        }
    }
}
