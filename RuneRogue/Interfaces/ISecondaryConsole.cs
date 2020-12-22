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
        // message is used if after secondary console completes, game should
        // do something additional based on interaction with secondary
        bool ProcessInput(RLKeyPress rLKeyPress, RLMouse rLMouse, out string message);

        // whether the choice the player makes in the console uses his or
        // her turn (unless cancelled with message "Cancelled")
        bool UsesTurn();

        RLConsole Console { get;  }
    }
}
