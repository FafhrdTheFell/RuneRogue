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
            DrawOnTargetedCells("targeting");
        }

        public void DrawOnTargetedCells(string colorPattern)
        {
            if (colorPattern != "targeting" &&
                colorPattern != "Elements" &&
                colorPattern != "Death")
            {
                throw new ArgumentException($"Invalid colorPattern {colorPattern}.");
            }
            DungeonMap dungeonMap = Game.DungeonMap;
            Player player = Game.Player;

            List<Monster> monstersSeen = dungeonMap.MonstersInFOV();

            RLColor highlightColor = Colors.Gold;
            foreach (Cell point in TargetCells())
            {
                if (!dungeonMap.IsInFov(point.X, point.Y))
                {
                    continue;
                }
                if (colorPattern == "targeting")
                {
                    if (point.X == _currentTarget.X && point.Y == _currentTarget.Y)
                    {
                        highlightColor = Colors.FloorTarget;
                    }
                    else
                    {
                        highlightColor = Colors.FloorHighlight;
                    }
                }
                else if (colorPattern == "Death")
                {
                    switch (Dice.Roll("1d3"))
                    {
                        case 1:
                            highlightColor = Colors.Poisoncloud1;
                            break;
                        case 2:
                            highlightColor = Colors.Poisoncloud2;
                            break;
                        case 3:
                            highlightColor = Colors.Poisoncloud3;
                            break;
                    }   
                }
                else if (colorPattern == "Elements")
                {
                    switch (Dice.Roll("1d4"))
                    {
                        case 1:
                            highlightColor = Swatch.DbDeepWater;
                            break;
                        case 2:
                            highlightColor = Colors.Gold;
                            break;
                        case 3:
                            highlightColor = Swatch.DbSky;
                            break;
                        case 4:
                            highlightColor = Swatch.DbBlood;
                            break;
                    }
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
            Point _newTarget = new Point
            {
                X = _currentTarget.X,
                Y = _currentTarget.Y
            };
            bool leftClick = rLMouse.GetLeftClick();
            bool enterPressed = false;
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
            }
            bool playerTargeted = (_newTarget.X == player.X && _newTarget.Y == player.Y);
            if (_newTarget == _currentTarget && !playerTargeted &&
                (leftClick || enterPressed))
            {
                _console.Clear();
                DungeonMap dungeonMap = Game.DungeonMap;
                //dungeonMap.ComputeFov(player.X, player.Y, player.Awareness, true);
                dungeonMap.Draw(_console, _nullConsole);
                DoEffectOnTarget();
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
                for (int i = 0; i < 8; i++)
                {
                    DrawOnTargetedCells(_effect);
                    for (int j = 0; j < 10; j++)
                    {
                        Game.DrawRoot();
                    }
                }

                int damage;
                foreach (Actor target in TargetActors())
                {
                    for (int i = 0; i < 4; i++)
                    {
                        damage = Dice.Roll("1d10");
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
                            break;
                        }
                    }
                }
            }
            if (_effect == "Death")
            {
                // not sure how to do the graphics timing better than these loops
                for (int i = 0; i < 8; i++)
                {
                    DrawOnTargetedCells(_effect);
                    for (int j = 0; j < 10; j++)
                    {
                        Game.DrawRoot();
                    }
                }
                
                foreach (Actor target in TargetActors())
                {
                    attackMessage.AppendFormat("{0} is immersed in poisonous vapors. ", target.Name);

                    if (target.IsUndead)
                    {
                        attackMessage.AppendFormat("{0} is immune to the poisonous vapors. ", target.Name);
                    }
                    else
                    {
                        Poison poison = new Poison();
                        poison.Target = target;
                        int totalDamage = Dice.Roll("4d10");
                        poison.Magnitude = Dice.Roll("1d3"); // damage
                        poison.Speed = poison.Magnitude * 2 + Dice.Roll("2d4"); // speed of activation
                        poison.Duration = totalDamage / poison.Magnitude; // # of activations
                        Game.SchedulingSystem.Add(poison);
                    }
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
