﻿using OpenTK.Input;
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

        public InputSystem()
        {

        }

        // returns the movement direction indicated by key press, or
        // Direction.None if no direction indicated.
        public Direction MoveDirection(RLNET.RLKeyPress keyPress)
        {
            Direction direction;
            if (keyPress == null)
            {
                throw new ArgumentException("keypress is null", "keyPress");
            }
            else
            {
                switch (keyPress.Key)
                {
                    case RLKey.Up:
                        direction = Direction.Up;
                        break;
                    case RLKey.Keypad8:
                        direction = Direction.Up;
                        break;
                    case RLKey.Down:
                        direction = Direction.Down;
                        break;
                    case RLKey.Keypad2:
                        direction = Direction.Down;
                        break;
                    case RLKey.Left:
                        direction = Direction.Left;
                        break;
                    case RLKey.Keypad4:
                        direction = Direction.Left;
                        break;
                    case RLKey.Right:
                        direction = Direction.Right;
                        break;
                    case RLKey.Keypad6:
                        direction = Direction.Right;
                        break;
                    case RLKey.Keypad7:
                        direction = Direction.UpLeft;
                        break;
                    case RLKey.Keypad9:
                        direction = Direction.UpRight;
                        break;
                    case RLKey.Keypad1:
                        direction = Direction.DownLeft;
                        break;
                    case RLKey.Keypad3:
                        direction = Direction.DownRight;
                        break;
                    case RLKey.H:
                        direction = Direction.Left;
                        break;
                    case RLKey.J:
                        direction = Direction.Down;
                        break;
                    case RLKey.K:
                        direction = Direction.Up;
                        break;
                    case RLKey.L:
                        direction = Direction.Right;
                        break;
                    case RLKey.Y:
                        direction = Direction.UpLeft;
                        break;
                    case RLKey.U:
                        direction = Direction.UpRight;
                        break;
                    case RLKey.B:
                        direction = Direction.DownLeft;
                        break;
                    case RLKey.N:
                        direction = Direction.DownRight;
                        break;
                    case RLKey.W:
                        direction = Direction.Up;
                        break;
                    case RLKey.A:
                        direction = Direction.Left;
                        break;
                    case RLKey.S:
                        direction = Direction.Down;
                        break;
                    case RLKey.D:
                        direction = Direction.Right;
                        break;
                    default:
                        direction = Direction.None;
                        break;
                }
            }
            return (Direction)direction;
        }

        public bool ShiftDown(RLKeyPress keyPress)
        {
            return keyPress.Shift;
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
