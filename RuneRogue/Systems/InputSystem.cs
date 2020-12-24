using OpenTK.Input;
using RLNET;
using RuneRogue.Core;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuneRogue.Systems
{
    public class InputSystem
    {

        public static Dictionary<RLKey, string> autoKeys = new Dictionary<RLKey, string>
        {
            [RLKey.Number1] = "item",
            [RLKey.Number2] = "shop",
            [RLKey.Number3] = "door",
            [RLKey.Number4] = "explorable"
        };

        public static Dictionary<RLKey, Direction> directionKeys = new Dictionary<RLKey, Direction>
        {
            [RLKey.Up] = Direction.Up,
            [RLKey.Keypad8] = Direction.Up,
            [RLKey.Down] = Direction.Down,
            [RLKey.Keypad2] = Direction.Down,
            [RLKey.Left] = Direction.Left,
            [RLKey.Keypad4] = Direction.Left,
            [RLKey.Right] = Direction.Right,
            [RLKey.Keypad6] = Direction.Right,
            [RLKey.Keypad7] = Direction.UpLeft,
            [RLKey.Keypad9] = Direction.UpRight,
            [RLKey.Keypad1] = Direction.DownLeft,
            [RLKey.Keypad3] = Direction.DownRight,
            [RLKey.H] = Direction.Left,
            [RLKey.J] = Direction.Down,
            [RLKey.K] = Direction.Up,
            [RLKey.L] = Direction.Right,
            [RLKey.Y] = Direction.UpLeft,
            [RLKey.U] = Direction.UpRight,
            [RLKey.B] = Direction.DownLeft,
            [RLKey.N] = Direction.DownRight,
            [RLKey.W] = Direction.Up,
            [RLKey.A] = Direction.Left,
            [RLKey.S] = Direction.Down,
            [RLKey.D] = Direction.Right
        };

        public InputSystem()
        {

        }

        public bool ShiftDown(RLKeyPress keyPress)
        {
            return keyPress.Shift;
        }
 
        public bool TravelKeyPressed(RLKeyPress keyPress)
        {
            return keyPress.Key == RLKey.T;
        }

        public bool RuneKeyPressed(RLKeyPress keyPress)
        {
            return keyPress.Key == RLKey.R;
        }

        public bool PickupKeyPressed(RLKeyPress keyPress)
        {
            return keyPress.Key == RLKey.G;
        }

        public bool QuitKeyPressed(RLKeyPress keyPress)
        {
            bool yesquit = false;
            if ((keyPress.Key == RLKey.X || keyPress.Key == RLKey.C)
                && keyPress.Control)
            {
                yesquit = true;
            }
            return yesquit;
        }
        public bool CloseDoorKeyPressed(RLKeyPress keyPress)
        {
            return keyPress.Key == RLKey.C;
        }
        public bool CancelKeyPressed(RLKeyPress keyPress)
        {
            return ((keyPress.Key == RLKey.Escape) || (keyPress.Key == RLKey.X));
        }

        public bool DescendStairs(RLKeyPress keyPress)
        {
            return (keyPress.Key == RLKey.Period && keyPress.Shift);
        }

        public bool WaitKey(RLKeyPress keyPress)
        {
            return (((keyPress.Key == RLKey.Period) && !(keyPress.Shift)) ||
                (keyPress.Key == RLKey.Number5));
        }
    }

}
