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
        private RLConsole _statConsole;
        private string _projectileType;
        private int _range;
        private int _minRange;
        private int _radius;
        private int _targetNumber = -1;
        private List<Cell> _targetableCells;

        private string[] _projectileTypes =
        {
            "line",
            "ball",
            "point",
            "missile"
        };

        public RLConsole StatConsole
        {
            get { return _statConsole; }
        }

        public int TargetNumber
        {
            get { return _targetNumber; }
            set { _targetNumber = value; }
        }

        public TargetingSystem(string projectiletype, int range, int radius = 5, int minRange = 1)
        {

            _console = new RLConsole(Game.MapWidth, Game.MapHeight);
            _statConsole = new RLConsole(Game.StatWidth, Game.MapHeight);

            _targetableCells = TargetableCells();
            InitializeNewTarget(projectiletype, range, radius, minRange);
        }

        public override void DrawConsole()
        {
            DungeonMap dungeonMap = Game.DungeonMap;
            Player player = Game.Player;

            _console.Clear();
            _statConsole.Clear();
            _statConsole.SetBackColor(0, 0, Game.StatWidth, Game.MapHeight, Swatch.DbOldStone);
            player.DrawStats(_statConsole);

            dungeonMap.ComputeFov(player.X, player.Y, player.Awareness, true);
            player.Draw(_console, dungeonMap);
            if (_currentTarget == _playerPosition)
            {
                dungeonMap.Draw(_console, _statConsole);
                player.Draw(_console, dungeonMap);
                return;
            }

            List<Cell> monsterCellsTargeted = TargetActorCells();

            dungeonMap.Draw(_console, _statConsole, highlightContentsCells: monsterCellsTargeted);
            player.Draw(_console, dungeonMap);

            RLColor highlightColor = Colors.Gold;
            List<Cell> targetCells = TargetCells();

            List<Monster> monstersSeen = dungeonMap.MonstersInFOV();

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

        public void InitializeNewTarget(string projectiletype, int range, int radius, int minRange)
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
            _range = range;
            _radius = radius;
            _minRange = minRange;
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
            DungeonMap dungeonMap = Game.DungeonMap;
            Point _newTarget = new Point
            {
                X = _currentTarget.X,
                Y = _currentTarget.Y
            };
            bool leftClick = rLMouse.GetLeftClick();
            bool enterPressed = false;
            bool cancelPressed = false;
            bool tabPressed = false;
            
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
                if (rLKeyPress.Key == RLKey.Tab)
                {
                    tabPressed = true;
                }
                cancelPressed = _inputSystem.CancelKeyPressed(rLKeyPress);
            }
            bool playerTargeted = (_newTarget.X == player.X && _newTarget.Y == player.Y);
            if (_newTarget == _currentTarget && !playerTargeted &&
                (leftClick || enterPressed))
            {
                if (Distance(_playerPosition, _newTarget) > _range)
                {
                    Game.MessageLog.Add("That target is too far away.");
                    return false;
                }
                if (Distance(_playerPosition, _newTarget) < _minRange)
                {
                    Game.MessageLog.Add("That target is too close by.");
                    return false;
                }
                _console.Clear();
                dungeonMap.Draw(_console, _statConsole);
                if (Game.PostSecondary is Instant)
                {
                    Instant nextSecondary = Game.PostSecondary as Instant;

                    nextSecondary.Source = player; // dungeonMap.GetCell(player.X, player.Y);
                    nextSecondary.Target = dungeonMap.GetCell(_newTarget.X, _newTarget.Y);
                }
                return true;
            }
            else if (_newTarget == _currentTarget && cancelPressed)
            {
                message = "Cancelled";
                return true;
            }
            else if (_newTarget == _currentTarget && playerTargeted &&
                (leftClick || enterPressed))
            {
                Game.MessageLog.Add("You cannot target yourself.");
            }
            else if (tabPressed)
            {
                int targetsAvailable = TargetableActors().Count();
                if (targetsAvailable == 0)
                {
                    return false;
                }
                TargetNumber++;
                if (TargetNumber >= targetsAvailable)
                {
                    TargetNumber -= targetsAvailable;
                }

                _newTarget = new Point();
                _newTarget.X = TargetableActors()[TargetNumber].X;
                _newTarget.Y = TargetableActors()[TargetNumber].Y;
            }
            if (_newTarget.X < 1 || _newTarget.X > Game.MapWidth ||
                _newTarget.Y < 1 || _newTarget.Y > Game.MapHeight)
            {
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

        // return cells that are being targeted based on shape and current target
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
                throw new ArgumentException($"projectiletype {_projectileType} not implemented.");
            }
            return cellsTargeted;
        }

        // returns all cells that are valid targets
        public List<Cell> TargetableCells()
        {
            DungeonMap dungeonMap = Game.DungeonMap;
            Player player = Game.Player;

            List<Cell> targetables = new List<Cell>();

            FieldOfView inPlayerRange = new FieldOfView(dungeonMap);
            inPlayerRange.ComputeFov(player.X, player.Y, Math.Min(_range, player.Awareness), true);
            foreach (Cell cell in dungeonMap.GetCellsInArea(player.X, player.Y, _range + 1))
            {
                if (inPlayerRange.IsInFov(cell.X, cell.Y))
                {
                    targetables.Add(cell);
                }
            }
            return targetables;
        }

        // returns all actors that are valid targets
        public List<Actor> TargetableActors()
        {
            DungeonMap dungeonMap = Game.DungeonMap;
            Player player = Game.Player;

            dungeonMap.ComputeFov(player.X, player.Y, Math.Min(_range, player.Awareness), true);
            List<Monster> monstersSeen = dungeonMap.MonstersInFOV();
            monstersSeen.Sort((x, y) => (100*x.X+x.Y).CompareTo(100*y.X+y.Y));
            List<Actor> actorTargetable = new List<Actor>();
            foreach(Monster m in monstersSeen)
            {
                actorTargetable.Add(m as Actor);
            }

            return actorTargetable;
        }

        // targeted cells containing actors
        public List<Cell> TargetActorCells()
        {
            DungeonMap dungeonMap = Game.DungeonMap;
            Player player = Game.Player;
            
            List<Cell> cells = new List<Cell>();

            //foreach (Cell cell in dungeonMap.GetCellsInRadius(player.X, player.Y, _range))
            foreach (Cell cell in TargetCells())
            {
                if (dungeonMap.GetMonsterAt(cell.X, cell.Y) != null)
                {
                    //Actor actor = dungeonMap.GetMonsterAt(cell.X, cell.Y) as Actor;
                    cells.Add(cell);
                } 
                else if (player.X == cell.X && player.Y == cell.Y)
                {
                    //Actor actor = player as Actor;
                    cells.Add(cell);
                }
            }
            return cells;
        }

    }
}
