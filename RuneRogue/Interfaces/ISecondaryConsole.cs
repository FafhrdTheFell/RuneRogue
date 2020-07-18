using RLNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuneRogue.Interfaces
{
    interface ISecondaryConsole
    {
        void DrawConsole();

        // process key press and return true iff finished with console
        bool ProcessKeyInput(RLKeyPress rLKeyPress);

        RLConsole Console { get;  }
    }
}
