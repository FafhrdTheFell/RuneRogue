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

        bool ProcessKeyInput(RLKeyPress rLKeyPress);

        RLConsole Console { get;  }
    }
}
