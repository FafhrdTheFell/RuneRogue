using Microsoft.SqlServer.Server;
using RLNET;
using RogueSharp;
using RogueSharp.DiceNotation;
using RuneRogue.Core;
using RuneRogue.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RuneRogue.Systems
{
    public class TargetingSystem : SecondaryConsole
    {

        private Point _currentTarget;
        private Point _playerPosition;
        private RLConsole _nullConsole;
        private string _projectileType;
        private string _effect;
        private int _range;
        private int _radius;

        private string[] _projectileTypes =
        {
            "line",
            "ball",
            "point"
        };

        private string[] _effectTypes =
        {
            "Elements",
            "Death"
        };

        private string[] _elements =
        {
            "fire",
            "lightning",
            "ice",
            "cold",
            "steam",
            "water",
            "air"
        };

        public TargetingSystem()
        {

            _console = new RLConsole(Game.MapWidth, Game.MapHeight);
            _nullConsole = new RLConsole(30, Game.MapHeight);

            //_playerPosition = new Point
            //{
            //    X = Game.Player.X,
            //    Y = Game.Player.Y
            //};

            InitializeNewTarget("ball", "Elements", 8, 3);
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
            
            List<Monster> monstersSeen = dungeonMap.MonstersInFOV();

            RLColor highlightColor;
            foreach (Cell point in TargetCells())
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

        public void InitializeNewTarget(string projectiletype, string effect, int range, int radius = 5)
        {
            //if (projectiletype == "line" || projectiletype == "ball" || projectiletype == "point")
            List<string> typesCheck = new List<string>(_projectileTypes);
            if (typesCheck.Contains(projectiletype))
            {
                _projectileType = projectiletype;

            }
            else
            {
                throw new ArgumentException($"Invalid projectiletype {projectiletype}.");
            }
            typesCheck = new List<string>(_effectTypes);
            if (typesCheck.Contains(effect))
            {
                _effect = effect;

            }
            else
            {
                throw new ArgumentException($"Invalid effect {effect}.");
            }
            if (projectiletype == "point")
            {
                radius = 1;
            }
            _effect = effect;
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
        public override bool ProcessInput(RLKeyPress rLKeyPress, RLMouse rLMouse)
        {
            Player player = Game.Player;
            if (rLMouse.GetLeftClick())
            {
                Point _newTarget = new Point
                {
                    X = rLMouse.X,
                    Y = rLMouse.Y
                };
                bool playerTargeted = (_newTarget.X == player.X && _newTarget.Y == player.Y);
                if (_newTarget == _currentTarget && !playerTargeted)
                {
                    _console.Clear();
                    DungeonMap dungeonMap = Game.DungeonMap;
                    //dungeonMap.ComputeFov(player.X, player.Y, player.Awareness, true);
                    dungeonMap.Draw(_console, _nullConsole);
                    DoEffectOnTarget();
                    return true;
                }
                if (Distance(_playerPosition, _newTarget) > _range)
                {
                    Game.MessageLog.Add("That target is too far away.");
                    _currentTarget = _playerPosition;
                    return false;
                }
                Game.DungeonMap.ComputeFov(player.X, player.Y, player.Awareness, true);
                if (!Game.DungeonMap.IsInFov(_newTarget.X, _newTarget.Y) || 
                    !Game.DungeonMap.IsTransparent(_newTarget.X, _newTarget.Y))
                {
                    Game.MessageLog.Add("You cannot see that target.");
                    _currentTarget = _playerPosition;
                    return false;
                }
                if (playerTargeted)
                {
                    Game.MessageLog.Add("You cannot target yourself.");
                    _currentTarget = _playerPosition;
                }
                else
                {
                    _currentTarget = _newTarget;
                    Game.MessageLog.Add("Target selected. Click again to finalize.");
                }
                return false;
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
            Player player = Game.Player;

            List<Cell> cellsTargeted = new List<Cell>();

            if (_projectileType == "line")
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
            //dungeonMap.ComputeFov(player.X, player.Y, player.Awareness, true);
            
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

        public bool DoEffectOnTarget()
        {
            StringBuilder attackMessage = new StringBuilder();
            if (_effect == "Elements")
            {
                int damage = 0;
                foreach (Actor target in TargetActors())
                {
                    damage = Dice.Roll("2d20");
                    string element = (string)Game.RandomArrayValue(_elements);
                    target.Health -= damage;

                    attackMessage.AppendFormat("{0} is blasted by {1}.", target.Name, element);
                    if (target.Health > 0)
                    {
                        attackMessage.AppendFormat(" {0} takes {1} damage. ", target.Name, damage);
                    }
                    else
                    {
                        attackMessage.AppendFormat(" {0} takes {1} damage, killing it. ", target.Name, damage);
                        CommandSystem.ResolveDeath(target, attackMessage);

                    }
                }
            }
            if (_effect == "Death")
            {
                foreach (Actor target in TargetActors())
                {
                    if (target.IsUndead)
                    {
                        attackMessage.AppendFormat("{0} is immune to the poisonous vapors. ", target.Name);
                        continue;
                    }
                    Poison poison = new Poison();
                    poison.Target = target;
                    int totalDamage = Dice.Roll("4d10");
                    poison.Magnitude = Dice.Roll("1d3"); // damage
                    poison.Speed = poison.Magnitude * 2 + Dice.Roll("2d4"); // speed of activation
                    poison.Duration = totalDamage / poison.Magnitude; // # of activations
                    Game.SchedulingSystem.Add(poison);

                    attackMessage.AppendFormat("{0} is immersed in poisonous vapors. ", target.Name);
                }
            }
            if (!string.IsNullOrWhiteSpace(attackMessage.ToString()))
            {
                Game.MessageLog.Add(attackMessage.ToString());
            }
            return true;
        }

        //// code from http://ericw.ca/notes/bresenhams-line-algorithm-in-csharp.html
        //public IEnumerable<Point> GetPointsOnLine(int x0, int y0, int x1, int y1)
        //{
        //    bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
        //    if (steep)
        //    {
        //        int t;
        //        t = x0; // swap x0 and y0
        //        x0 = y0;
        //        y0 = t;
        //        t = x1; // swap x1 and y1
        //        x1 = y1;
        //        y1 = t;
        //    }
        //    if (x0 > x1)
        //    {
        //        int t;
        //        t = x0; // swap x0 and x1
        //        x0 = x1;
        //        x1 = t;
        //        t = y0; // swap y0 and y1
        //        y0 = y1;
        //        y1 = t;
        //    }
        //    int dx = x1 - x0;
        //    int dy = Math.Abs(y1 - y0);
        //    int error = dx / 2;
        //    int ystep = (y0 < y1) ? 1 : -1;
        //    int y = y0;
        //    for (int x = x0; x <= x1; x++)
        //    {
        //        yield return new Point((steep ? y : x), (steep ? x : y));
        //        error = error - dy;
        //        if (error < 0)
        //        {
        //            y += ystep;
        //            error += dx;
        //        }
        //    }
        //    yield break;
        //}

    }
}
