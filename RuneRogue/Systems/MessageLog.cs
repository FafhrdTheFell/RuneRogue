using System;
using System.Text;
using System.Collections.Generic;
using RLNET;
using RuneRogue.Core;
using OpenTK.Graphics.OpenGL;

namespace RuneRogue.Systems
{
    // Represents a queue of messages that can be added to and drawn to a RLConsole
    public class MessageLog
    {
        // Define the maximum number of lines to store
        private static int _maxLines = 12;
        private static int _lineWidth = 80;

        // Use a Queue to keep track of the lines of text
        // The first line added to the log will also be the first removed
        private readonly Queue<string> _lines;

        public MessageLog()
        {
            _maxLines = Game.MessageLines - 2;
            _lineWidth = Game.MessageWidth - 8;
            _lines = new Queue<string>();
        }

        // Add a line to the MessageLog queue
        public void Add(string message)
        {
            List<string> wrappedMessage = WrapText(" * " + message, _lineWidth);
            foreach (string line in wrappedMessage.ToArray())
            {
                _lines.Enqueue(line);
            }
            //_lines.Enqueue(message);

            // When exceeding the maximum number of lines remove the oldest one.
            while (_lines.Count > _maxLines)
            {
                _lines.Dequeue();
            }
        }

        // Draw each line of the MessageLog queue to the console
        public void Draw(RLConsole console)
        {
            string[] lines = _lines.ToArray();
            for (int i = 0; i < lines.Length; i++)
            {
                console.Print(1, i + 1, lines[i], Colors.Text);
            }
        }

        static List<string> WrapText(string text, int linewidth)
        {
            string[] originalLines = text.Split(new string[] { " " },
                StringSplitOptions.None);

            List<string> wrappedLines = new List<string>();

            StringBuilder actualLine = new StringBuilder();
            int actualWidth = 0;

            foreach (string word in originalLines)
            {
                actualWidth += word.Length + 1;

                if (actualWidth > linewidth)
                {
                    wrappedLines.Add(actualLine.ToString());
                    actualLine.Clear();
                    actualLine.Append("   " + word + " ");
                    actualWidth = word.Length + 4;
                }
                {
                    actualLine.Append(word + " ");
                }
            }

            if (actualLine.Length > 0)
                wrappedLines.Add(actualLine.ToString());

            return wrappedLines;
        }
    }

}
