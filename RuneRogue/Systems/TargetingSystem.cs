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
            "point",
            "missile"
        };

        private string[] _effectTypes =
        {
            "Elements",
            "Death",
            "Iron"
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

        public void DrawOnTargetedCells(string colorPattern, int? whichcell=null, char? symbol=null)
        {
            
            if (colorPattern != "targeting" &&
                colorPattern != "Elements" &&
                colorPattern != "Death" &&
                colorPattern != "Iron")
            {
                throw new ArgumentException($"Invalid colorPattern {colorPattern}.");
            }
            DungeonMap dungeonMap = Game.DungeonMap;
            Player player = Game.Player;

            _console.Clear();
            dungeonMap.Draw(_console, _nullConsole);
            player.Draw(_console, dungeonMap);

            List<Monster> monstersSeen = dungeonMap.MonstersInFOV();

            RLColor highlightColor = Colors.Gold;
            List<Cell> targetCells = TargetCells();
            if (whichcell.HasValue)
            {
                targetCells = targetCells.GetRange((int)whichcell, 1);
            }
            foreach (Cell point in targetCells)
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
                else if (colorPattern == "Iron")
                {
                    highlightColor = Swatch.DbBrightMetal;
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
                    if (symbol.HasValue)
                    {
                        _console.Set(point.X, point.Y, highlightColor, Colors.FloorBackgroundFov, (char)symbol);
                    }
                    else
                    {
                        _console.Set(point.X, point.Y, Colors.FloorFov, highlightColor, '.');
                    }
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
                        int totalDamage = Dice.Roll("4d10");
                        int activationDamage = Dice.Roll("1d4"); // damage
                        int poisonSpeed = activationDamage * 2 + Dice.Roll("2d4"); // speed of activation
                        Poison poison = new Poison(target, totalDamage, poisonSpeed, activationDamage);
                        //Game.SchedulingSystem.Add(poison);
                    }
                }
            }
            if (_effect == "Iron")
            {
                Player player = Game.Player;
                float dx = _playerPosition.X - _currentTarget.X;
                float dy = _playerPosition.Y - _currentTarget.Y;
                char missileChar;
                if (Math.Abs(dx) / Math.Abs(dy) > 1.5)
                {
                    missileChar = '-';
                }
                else if (Math.Abs(dx) / Math.Abs(dy) < 0.66)
                {
                    missileChar = '|';
                }
                else if ((dx > 0 && dy > 0) || (dx < 0 && dy < 0))
                {
                    missileChar = '\\';
                }
                else
                {
                    missileChar = '/';
                }
                for (int i = 0; i < TargetCells().Count; i++)
                {
                    DrawOnTargetedCells(_effect, i, missileChar);
                    for (int j = 0; j < 10; j++)
                    {
                        Game.DrawRoot();
                    }
                }

                int damage;
                foreach (Actor target in TargetActors())
                {
                    StringBuilder discardMessage = new StringBuilder();
                    damage = CommandSystem.ResolveArmor(target, player, Runes.BonusToDamageIron, discardMessage);
                    target.Health -= damage;

                    if (damage > 0)
                    {
                        attackMessage.AppendFormat("{0} is perforated.", target.Name);
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
                    else
                    {
                        attackMessage.AppendFormat("The dart bounces off {0}'s armor.", target.Name);
                        break;
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(attackMessage.ToString()))
            {
                Game.MessageLog.Add(attackMessage.ToString());
            }
            return true;
        }

    }
}
