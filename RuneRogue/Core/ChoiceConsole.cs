using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RLNET;
using RuneRogue.Interfaces;
using RuneRogue.Systems;

namespace RuneRogue.Core
{
    public class ChoiceConsole : ISecondaryConsole
    {
        protected RLConsole _console;
        protected int _numOptions;

        protected readonly int _verticalOffset = 4;
        protected readonly int _horizontalOffset = 4;

        protected readonly int _leftSelectButtonOffset = 6;
        protected readonly int _detailsColumnOffset = 72;
        protected int _descriptionOffset = 4;
        // selection = -1 if X/exit should be selected, the default initial position
        protected int _selection;
        protected string _choiceDescription;

        public int NumOptions
        {
            get { return MenuOptions().Count(); }
            //protected set { _numOptions = value; }
        }

        public virtual List<string> MenuOptions()
        {
            return new List<string>();
        }

        public virtual List<string> AdditionalDetails()
        {
            return new List<string>();
        }

        public virtual List<int> OptionsInactive()
        {
            return new List<int>();
        }

        // a choiceconsole presents a list of options to the player and
        // allows him or her to select one. MenuOptions, AdditionalDetails,
        // OptionsInactive need to be overrides
        public ChoiceConsole()
        {
            _console = new RLConsole(Game.MapWidth, Game.MapHeight);
            _selection = -1;
        }

        public void DrawOption(int option)
        {
            string keyChoice = (option + 1).ToString();
            string atoz = "ABCDEFGHIJKLMNOPQRSTUVW";
            if (option > 9)
            {
                keyChoice = atoz[option - 10].ToString();
            }
            RLColor textColor = Colors.Text;
            if (OptionsInactive().Contains(option))
            {
                textColor = Colors.TextInactive;
            }
            if (_selection == option)
            {
                _console.Print(_horizontalOffset, _descriptionOffset + _verticalOffset + 2 * option,
               ">", Colors.TextHeading);
            }
            _console.Print(_horizontalOffset + 2, _descriptionOffset + _verticalOffset + 2 * option,
                "(" + keyChoice + ")", textColor);
            _console.Print(_horizontalOffset + _leftSelectButtonOffset, _descriptionOffset + _verticalOffset + 2 * option,
                MenuOptions()[option], textColor);
            _console.Print(_detailsColumnOffset, _descriptionOffset + _verticalOffset + 2 * option,
                AdditionalDetails()[option], textColor);
        }

        public virtual void DrawConsole()
        {
            _console.Clear();
            _console.SetBackColor(0, 0, Game.MapWidth, Game.MapHeight, Swatch.Compliment);

            _console.Print(_horizontalOffset, _verticalOffset, _choiceDescription, Colors.TextHeading);

            for (int i = 0; i < NumOptions; i++)
            {
                DrawOption(i);
            }
            if (_selection == -1)
            {
                _console.Print(_horizontalOffset, _descriptionOffset + 1 + _verticalOffset + 2 * NumOptions,
                ">", Colors.TextHeading);
            }
            _console.Print(_horizontalOffset + 2, _descriptionOffset + 1 + _verticalOffset + 2 * NumOptions,
                "(X) Cancel.", Colors.Text);
        }

        // process key press and return true iff finished with console
        public virtual bool ProcessInput(RLKeyPress rLKeyPress, RLMouse rLMouse, out string message)
        {
            message = "";
            int choiceNum = -1;
            if (rLMouse.GetLeftClick())
            {
                if (rLMouse.X >= _horizontalOffset && rLMouse.X <= 80)
                {
                    if (rLMouse.Y - 4 - _verticalOffset + 2 - NumOptions * 2 == 3)
                    {
                        // exit row pressed
                        message = "Cancelled";
                        return true;
                    }
                    if ((rLMouse.Y - 4 - _verticalOffset) % 2 == 0)
                    {
                        choiceNum = 1 + (rLMouse.Y - 4 - _verticalOffset) / 2;
                        if (choiceNum > NumOptions || choiceNum < 1)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }

                }
            }
            else if (rLKeyPress != null)
            {
                InputSystem InputSystem = Game.InputSystem;
                
                if (InputSystem.directionKeys.TryGetValue(rLKeyPress.Key, out Direction direction) ||
                    rLKeyPress.Key == RLKey.Tab)
                {
                    if (OptionsInactive().Count == MenuOptions().Count)
                    {
                        // no active options
                        return false;
                    }
                    int delta;
                    if (direction == Direction.Up)
                    {
                        delta = -1;
                    }
                    else if (direction == Direction.Down || rLKeyPress.Key == RLKey.Tab)
                    {
                        delta = 1;
                    }
                    else
                    {
                        return false;
                    }
                    bool cycle = true;
                    while (cycle)
                    {
                        _selection += delta;
                        if (_selection == NumOptions) _selection = -1;
                        else if (_selection < -1) _selection += NumOptions + 1;
                        if (!OptionsInactive().Contains(_selection))
                        {
                            cycle = false;
                        }
                    }
                    return false;
                }
                else if (rLKeyPress.Key == RLKey.Enter)
                {
                    choiceNum = _selection + 1;
                    if (_selection == -1)
                    {
                        message = "Cancelled";
                        return true;
                    }
                }
                else if (rLKeyPress.Key == RLKey.X || rLKeyPress.Key == RLKey.R || rLKeyPress.Key == RLKey.Escape)
                {
                    _selection = -1;
                    message = "Cancelled";
                    return true;
                }
                else
                {
                    bool isNumber = int.TryParse(rLKeyPress.Char.ToString(), out choiceNum);
                    if (!isNumber || choiceNum > NumOptions || choiceNum < 1)
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
            bool success = ProcessChoice(choiceNum - 1);
            if (success)
            {
                _selection = -1;
            }
            return success;
        }

        public virtual bool ProcessChoice(int choiceIndex)
        {
            return false;
        }

        public virtual bool UsesTurn()
        {
            return false;
        }

        public RLConsole Console
        {
            get { return _console; }
        }
    }
}
