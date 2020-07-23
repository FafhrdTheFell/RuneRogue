using RLNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuneRogue.Interfaces
{
    public interface ISecondaryConsole
    {
        void DrawConsole();

        // process key press and return true iff finished with console
        bool ProcessInput(RLKeyPress rLKeyPress, RLMouse rLMouse);

        RLConsole Console { get;  }
    }
}
