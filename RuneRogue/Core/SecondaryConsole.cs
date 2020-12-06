using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RLNET;

namespace RuneRogue.Core
{
    public class SecondaryConsole
    {
        protected RLConsole _console;
        protected int _numOptions;

        protected readonly int _verticalOffset = 4;
        protected readonly int _horizontalOffset = 4;

        public int NumOptions
        {
            get { return _numOptions; }
            private set { _numOptions = value; }
        }

        public SecondaryConsole()
        {
            _console = new RLConsole(Game.MapWidth, Game.MapHeight);
        }

        public virtual void DrawConsole()
        {

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
                if (rLKeyPress.Char == null && rLKeyPress.Key != RLKey.Escape)
                {
                    return false;
                }
                if (rLKeyPress.Key == RLKey.X || rLKeyPress.Key == RLKey.R || rLKeyPress.Key == RLKey.Escape)
                {
                    message = "Cancelled";
                    return true;
                }

                bool isNumber = int.TryParse(rLKeyPress.Char.ToString(), out choiceNum);
                if (!isNumber || choiceNum > NumOptions || choiceNum < 1)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            bool success = ProcessChoice(choiceNum - 1);
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
