using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RLNET;

namespace RuneRogue.Core
{
    public class InputConsole : SecondaryConsole
    {
        private string _name;
        private int _maxChars;

        public InputConsole()
        {
            _console = new RLConsole(Game.MapWidth, Game.MapHeight);
            _name = ReadPreviousName();
            _maxChars = Game.StatWidth - 6;

        }

        public override void DrawConsole()
        {
            
            _console.Print(1, 3, $"What is your name, hopeful Rune-Lord-to-be?", Colors.Text);
            _console.Print(5, 5, $"{_name}", Colors.Text);
            int spacesLeft = _maxChars - _name.Length;
            for (int x = 4 + _name.Length + 1; x <= _maxChars + 5; x++)
            {
                _console.Print(x, 5, $"_", Swatch.Alternate);
            }
        }

        // process key press and return true iff finished with console
        public override bool ProcessInput(RLKeyPress rLKeyPress, RLMouse rLMouse, out string message)
        {
            message = "";
            if (rLKeyPress != null)
            {
                if (rLKeyPress.Char != null)
                {
                Char c = (Char)rLKeyPress.Char;
                if (Char.IsLetterOrDigit(c) || Char.IsWhiteSpace(c))
                    {
                        if (_name.Length < _maxChars)
                        {
                            _name += c;
                        }
                    }
                }
                else if (rLKeyPress.Key == RLKey.BackSpace && _name.Length > 0)
                {
                    _name = _name.Substring(0, _name.Length - 1);
                }
                else if (rLKeyPress.Key == RLKey.Enter && _name.Length > 0)
                {
                    WriteCurrentName(_name);
                    message = _name;
                    return true;
                }
            }
            else if (rLMouse.GetLeftClick() && _name.Length > 0)
            {
                WriteCurrentName(_name);
                message = _name;
                return true;
            }
            return false;
        }

        private string ReadPreviousName()
        {
            string name;
            if (File.Exists(Game.NameFile))
            {
                name = File.ReadAllText(Game.NameFile);
            }
            else
            {
                name = "Rogue";
            }
            return name;
        }

        private void WriteCurrentName(string name)
        {
            File.WriteAllText(Game.NameFile, name);
        }

    }
}
