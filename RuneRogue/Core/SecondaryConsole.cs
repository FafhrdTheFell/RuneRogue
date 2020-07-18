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

        public SecondaryConsole()
        {
            _console = new RLConsole(Game.MapWidth, Game.MapHeight);
        }

        public virtual void DrawConsole()
        {

        }

        // process key press and return true iff finished with console
        public virtual bool ProcessKeyInput(RLKeyPress rLKeyPress)
        {
            if (rLKeyPress.Char == null)
            {
                return false;
            }
            if (rLKeyPress.Key == RLKey.X)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public RLConsole Console
        {
            get { return _console; }
        }
    }
}
