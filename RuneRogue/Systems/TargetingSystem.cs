using RLNET;
using RogueSharp;
using RuneRogue.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using RuneRogue.Interfaces;

namespace RuneRogue.Systems
{
    public class TargetingSystem : ISecondaryConsole
    {
        private RLConsole _console;
        private Point _currentTarget;
        private Point _playerPosition;
        private readonly RLConsole _statConsole;
        private string _projectileType;
        private int _range;
        private int _minRange;
        private int _radius;
        private int _targetNumber = -1;
        private readonly List<Cell> _targetableCells;

        private static string[] _projectileTypes =
        {
            "line",
            "ball",
            "point",
            "missile",
            "travel-info"
        };

        public Cell Target
        {
            get { return Game.DungeonMap.GetCell(_currentTarget.X, _currentTarget.Y); }
        }

        public RLConsole Console
        {
            get { return _console; }
        }

        public bool UsesTurn()
        {
            return false;
        }


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

        public void DrawConsole()
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
                _console.Set(player.X, player.Y, player.Color, Colors.FloorTarget, player.Symbol);
                return;
            }

            List<Cell> monsterCellsTargeted = TargetActorCells();

            dungeonMap.Draw(_console, _statConsole, highlightContentsCells: monsterCellsTargeted);
            player.Draw(_console, dungeonMap);

            RLColor highlightColor = Colors.Gold;
            List<Cell> targetCells = TargetCells();
            // if target cells does not extend to current  target the line has
            // gone through a wall. add current target to target cells so that it
            // gets drawn
            if (!targetCells.Contains(dungeonMap.GetCell(_currentTarget.X, _currentTarget.Y)))
            {
                targetCells.Add(dungeonMap.GetCell(_currentTarget.X, _currentTarget.Y));
            }

            List<Monster> monstersSeen = dungeonMap.MonstersInFOV();

            foreach (Cell point in targetCells)
            {
                if (!dungeonMap.IsInFov(point.X, point.Y) && _projectileType != "travel-info")
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
                    _console.Set(point.X, point.Y, player.Color, highlightColor, player.Symbol);
                }
                else if (!dungeonMap.IsInFov(point.X, point.Y))
                {
                    _console.Set(point.X, point.Y, Colors.WallFov, highlightColor, 'X');
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
                        //_console.Set(point.X, point.Y, monster.Color, highlightColor, monster.Symbol);
                        _console.Set(point.X, point.Y, Colors.FloorFov, highlightColor, '.');
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
                if (Game.DungeonMap.GetShopAt(point.X, point.Y) != null && dungeonMap.GetCell(point.X, point.Y).IsExplored)
                {
                    Shop shop = Game.DungeonMap.GetShopAt(point.X, point.Y);
                    _console.Set(point.X, point.Y, shop.Color, highlightColor, shop.Symbol);
                }
                else if (Game.DungeonMap.StairsDown.X == point.X && Game.DungeonMap.StairsDown.Y == point.Y
                    && dungeonMap.GetCell(point.X, point.Y).IsExplored)
                {
                    _console.Set(point.X, point.Y, Colors.FloorFov, highlightColor, '>');
                }
                else if (Game.DungeonMap.StairsUp.X == point.X && Game.DungeonMap.StairsUp.Y == point.Y
                    && dungeonMap.GetCell(point.X, point.Y).IsExplored)
                {
                    _console.Set(point.X, point.Y, Colors.FloorFov, highlightColor, '<');
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
        public bool ProcessInput(RLKeyPress rLKeyPress, RLMouse rLMouse, out string message)
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
            bool playerTargeted = (_newTarget.X == player.X && _newTarget.Y == player.Y);

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
                InputSystem InputSystem = Game.InputSystem;
                if (InputSystem.directionKeys.ContainsKey(rLKeyPress.Key))
                {
                    Direction direction = InputSystem.directionKeys[rLKeyPress.Key];
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
                cancelPressed = InputSystem.CancelKeyPressed(rLKeyPress);
            }
            if ((leftClick || enterPressed) &&
                _newTarget == _currentTarget && !playerTargeted)
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
                if (_projectileType == "travel-info")
                {
                    message = "travel";
                }
                else if (Game.PostSecondary is Instant)
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
                int targetsAvailable;
                if (_projectileType == "travel-info")
                {
                    targetsAvailable = TargetableLandmarks().Count(); 
                }
                else
                {
                    targetsAvailable = TargetableActors().Count();
                }
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
                if (_projectileType == "travel-info")
                {
                    _newTarget.X = TargetableLandmarks()[TargetNumber].X;
                    _newTarget.Y = TargetableLandmarks()[TargetNumber].Y;
                }
                else
                {
                    _newTarget.X = TargetableActors()[TargetNumber].X;
                    _newTarget.Y = TargetableActors()[TargetNumber].Y;
                }
            }
            if (_newTarget.X < 1 || _newTarget.X > Game.MapWidth ||
                _newTarget.Y < 1 || _newTarget.Y > Game.MapHeight)
            {
                return false;
            }
            if (_projectileType != "travel-info")
            {
                Game.DungeonMap.ComputeFov(player.X, player.Y, player.Awareness, true);
                if (!Game.DungeonMap.IsInFov(_newTarget.X, _newTarget.Y) ||
                    !Game.DungeonMap.IsTransparent(_newTarget.X, _newTarget.Y))
                {
                    Game.MessageLog.Add("You would not be able to see that target.");
                    return false;
                }
            }
            _currentTarget = _newTarget;
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
                // check for first wall
                int i;
                for (i = 0; i < cellsTargeted.Count; i++)
                {
                    if (!cellsTargeted[i].IsTransparent)
                    {
                        break;
                    }
                }
                cellsTargeted.RemoveRange(i, cellsTargeted.Count - i);
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
            else if (_projectileType == "point" || _projectileType == "travel-info")
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
            inPlayerRange.ComputeFov(player.X, player.Y, player.Awareness, true);
            foreach (Cell cell in dungeonMap.GetCellsInArea(player.X, player.Y, _range + 1))
            {
                if (inPlayerRange.IsInFov(cell.X, cell.Y))
                {
                    targetables.Add(cell);
                }
            }
            return targetables;
        }

        public List<Cell> TargetableLandmarks()
        {
            DungeonMap dungeonMap = Game.DungeonMap;
            List<Cell> exploredLandmarks = new List<Cell>
            {
                dungeonMap.GetCell(dungeonMap.StairsUp.X, dungeonMap.StairsUp.Y)
            };
            if (dungeonMap.GetCell(dungeonMap.StairsDown.X, dungeonMap.StairsDown.Y).IsExplored)
            {
                exploredLandmarks.Add(dungeonMap.GetCell(dungeonMap.StairsDown.X, dungeonMap.StairsDown.Y));
            }
            foreach(Shop shop in dungeonMap.Shops.
                Where(s => dungeonMap.GetCell(s.X, s.Y).IsExplored))
            {
                exploredLandmarks.Add(dungeonMap.GetCell(shop.X, shop.Y));
            }
            exploredLandmarks.Sort((x, y) => (100 * x.X + x.Y).CompareTo(100 * y.X + y.Y));
            return exploredLandmarks;

        }

        // returns all actors that are valid targets
        public List<Actor> TargetableActors()
        {
            DungeonMap dungeonMap = Game.DungeonMap;
            Player player = Game.Player;

            dungeonMap.ComputeFov(player.X, player.Y, player.Awareness, true);
            List<Monster> monstersSeen = dungeonMap.MonstersInFOV();
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
