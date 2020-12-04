using Microsoft.SqlServer.Server;
using OpenTK.Input;
using RLNET;
using RogueSharp;
using RogueSharp.DiceNotation;
using RuneRogue.Core;
using RuneRogue.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;

namespace RuneRogue.Systems
{
    public class TargetingSystem : SecondaryConsole
    {

        private Point _currentTarget;
        private Point _playerPosition;
        private RLConsole _nullConsole;
        private string _projectileType;
        private int _range;
        private int _radius;

        private string[] _projectileTypes =
        {
            "line",
            "ball",
            "point",
            "missile"
        };


        public TargetingSystem(string projectiletype, int range, int radius = 5)
        {

            _console = new RLConsole(Game.MapWidth, Game.MapHeight);
            _nullConsole = new RLConsole(30, Game.MapHeight);

            InitializeNewTarget(projectiletype, range, radius);
        }

        public override void DrawConsole()
        {
            DungeonMap dungeonMap = Game.DungeonMap;
            Player player = Game.Player;

            _console.Clear();
            dungeonMap.ComputeFov(player.X, player.Y, player.Awareness, true);
            dungeonMap.Draw(_console, _nullConsole);
            player.Draw(_console, dungeonMap);
            if (_currentTarget == _playerPosition)
            {
                return;
            }
            DrawOnTargetedCells();
        }

        public void DrawOnTargetedCells()
        {
            DungeonMap dungeonMap = Game.DungeonMap;
            Player player = Game.Player;

            _console.Clear();
            dungeonMap.Draw(_console, _nullConsole);
            player.Draw(_console, dungeonMap);

            List<Monster> monstersSeen = dungeonMap.MonstersInFOV();

            RLColor highlightColor = Colors.Gold;
            List<Cell> targetCells = TargetCells();

            foreach (Cell point in targetCells)
            {
                if (!dungeonMap.IsInFov(point.X, point.Y))
                {
                    continue;
                }
                if (point.X == _currentTarget.X && point.Y == _currentTarget.Y)
                {
                    highlightColor = Colors.FloorTarget;
                }
                else
                {
                    highlightColor = Colors.FloorHighlight;
                }
                if (point.X == player.X && point.Y == player.Y)
                {
                    player.Draw(_console, dungeonMap);
                }
                else if (Game.DungeonMap.GetMonsterAt(point.X, point.Y) != null)
                {
                    Monster monster = Game.DungeonMap.GetMonsterAt(point.X, point.Y);
                    if (monstersSeen.Contains(monster))
                    {
                        _console.Set(point.X, point.Y, monster.Color, highlightColor, monster.Symbol);
                    }
                    else
                    {
                        _console.Set(point.X, point.Y, monster.Color, highlightColor, monster.Symbol);
                    }
                }
                else if (point.IsWalkable)
                {
                    _console.Set(point.X, point.Y, Colors.FloorFov, highlightColor, '.');
                }
                else
                {
                    _console.Set(point.X, point.Y, Colors.WallFov, highlightColor, '#');
                }
            }
        }

        public void InitializeNewTarget(string projectiletype, int range, int radius = 5)
        {
            List<string> typesCheck = new List<string>(_projectileTypes);
            if (typesCheck.Contains(projectiletype))
            {
                _projectileType = projectiletype;

            }
            else
            {
                throw new ArgumentException($"Invalid projectiletype {projectiletype}.");
            }
            if (projectiletype == "point")
            {
                radius = 1;
            }
            _range = range;
            _radius = radius;
            _currentTarget = new Point
            {
                X = Game.Player.X,
                Y = Game.Player.Y
            };
            _playerPosition = new Point
            {
                X = Game.Player.X,
                Y = Game.Player.Y
            };
        }

        private int Distance(Point p, Point q)
        {
            DungeonMap dungeonMap = Game.DungeonMap;
            int d = 0;
            foreach (Cell point in dungeonMap.GetCellsAlongLine(p.X, p.Y, q.X, q.Y))
            {
                d++;
            }
            // subtract 1 because starting point is player
            return d - 1;
        }

        // process key press and return true iff finished with console
        public override bool ProcessInput(RLKeyPress rLKeyPress, RLMouse rLMouse, out string message)
        {
            message = "";
            Player player = Game.Player;
            Point _newTarget = new Point
            {
                X = _currentTarget.X,
                Y = _currentTarget.Y
            };
            bool leftClick = rLMouse.GetLeftClick();
            bool enterPressed = false;
            bool cancelPressed = false;
            if (leftClick)
            {
                _newTarget = new Point
                {
                    X = rLMouse.X,
                    Y = rLMouse.Y
                };
            }
            if (rLKeyPress != null)
            {
                InputSystem _inputSystem = new InputSystem();
                Direction direction = _inputSystem.MoveDirection(rLKeyPress);
                if (direction != Direction.None)
                {
                    _newTarget.X += Game.CommandSystem.DirectionToCoordinates(direction)[0];
                    _newTarget.Y += Game.CommandSystem.DirectionToCoordinates(direction)[1];
                }
                if (rLKeyPress.Key == RLKey.Enter)
                {
                    enterPressed = true;
                }
                cancelPressed = _inputSystem.CancelKeyPressed(rLKeyPress);
            }
            bool playerTargeted = (_newTarget.X == player.X && _newTarget.Y == player.Y);
            if (_newTarget == _currentTarget && !playerTargeted &&
                (leftClick || enterPressed))
            {
                _console.Clear();
                DungeonMap dungeonMap = Game.DungeonMap;
                dungeonMap.Draw(_console, _nullConsole);
                if (Game.PostSecondary is Instant)
                {
                    Instant nextSecondary = Game.PostSecondary as Instant;

                    nextSecondary.Origin = dungeonMap.GetCell(player.X, player.Y);
                    nextSecondary.Target = dungeonMap.GetCell(_newTarget.X, _newTarget.Y);
                }
                return true;
            }
            else if (_newTarget == _currentTarget && (cancelPressed))
            {
                message = "Cancelled";
                return true;
            }
            else if (_newTarget == _currentTarget && playerTargeted &&
                (leftClick || enterPressed))
            {
                Game.MessageLog.Add("You cannot target yourself.");
            }
            if (_newTarget.X < 1 || _newTarget.X > Game.MapWidth ||
                _newTarget.Y < 1 || _newTarget.Y > Game.MapHeight)
            {
                return false;
            }
            if (Distance(_playerPosition, _newTarget) > _range)
            {
                Game.MessageLog.Add("That target would be too far away.");
                return false;
            }
            Game.DungeonMap.ComputeFov(player.X, player.Y, player.Awareness, true);
            if (!Game.DungeonMap.IsInFov(_newTarget.X, _newTarget.Y) ||
                !Game.DungeonMap.IsTransparent(_newTarget.X, _newTarget.Y))
            {
                Game.MessageLog.Add("You would not be able to see that target.");
                return false;
            }
            else
            {
                _currentTarget = _newTarget;
            }
            return false;
        }

        public Point Target()
        {
            return _currentTarget;
        }

        public List<Cell> TargetCells()
        {
            DungeonMap dungeonMap = Game.DungeonMap;

            List<Cell> cellsTargeted = new List<Cell>();

            if (_projectileType == "line" || _projectileType == "missile")
            {
                cellsTargeted = dungeonMap.GetCellsAlongLine(_playerPosition.X, _playerPosition.Y,
                    _currentTarget.X, _currentTarget.Y).ToList();
                // Contains player cell, drop it.
                cellsTargeted.RemoveAt(0);
            }
            else if (_projectileType == "ball")
            {
                FieldOfView targetFOV = new FieldOfView(dungeonMap);
                targetFOV.ComputeFov(_currentTarget.X, _currentTarget.Y, _radius + 1, true);

                foreach (Cell cell in dungeonMap.GetCellsInRadius(_currentTarget.X, _currentTarget.Y, _radius))
                {
                    if (targetFOV.IsInFov(cell.X, cell.Y))
                    {
                        cellsTargeted.Add(cell);
                    }
                }
            }
            else if (_projectileType == "point")
            {
                cellsTargeted.Add(dungeonMap.GetCell(_currentTarget.X, _currentTarget.Y));
            }
            else
            {
                throw new ArgumentException("projectiletype TargetCells not implemented.");
            }
            return cellsTargeted;
        }

        public List<Actor> TargetActors()
        {
            DungeonMap dungeonMap = Game.DungeonMap;
            Player player = Game.Player;
            
            List<Actor> actors = new List<Actor>();

            foreach (Cell cell in TargetCells())
            {
                if (dungeonMap.GetMonsterAt(cell.X, cell.Y) != null)
                {
                    Actor actor = dungeonMap.GetMonsterAt(cell.X, cell.Y) as Actor;
                    actors.Add(actor);
                } 
                else if (player.X == cell.X && player.Y == cell.Y)
                {
                    Actor actor = player as Actor;
                    actors.Add(actor);
                }
            }
            return actors;
        }

    }
}
