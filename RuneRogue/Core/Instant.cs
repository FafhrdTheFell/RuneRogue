using Microsoft.SqlServer.Server;
using OpenTK.Input;
using RLNET;
using RogueSharp;
using RogueSharp.DiceNotation;
using RuneRogue.Core;
using RuneRogue.Effects;
using RuneRogue.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;

// The Instant class both applies an instantly occuring effect, such as being attacked by
// a spell / rune-magic or missile weapon, and animates it. Technically it is a secondary
// console that replaces the primary console temporarily. It replaces it by default for
// _totalSteps draw steps. DrawEffect(x) is the code to draw step x. Typically the same
// drawing occurs for several (5) steps; otherwise it is too fast to easily be seen. When the
// player presses a key or _totalSteps have been drawn, DoEffectOnTarget() is called and
// actually executes the effect.

namespace RuneRogue.Core
{
    public class Instant : SecondaryConsole
    {

        private Cell _origin;
        private Cell _target;
        private RLConsole _nullConsole;
        private string _projectileType;
        private string _effect;
        private int _radius;
        private int _step;
        private int _animateSteps = 5;
        private int _totalSteps = 36;

        private string[] _projectileTypes =
        {
            "line", // hits everything on a line: death ray
            "ball", // explodes in a radius
            "point", // hits a particular cell
            "missile" // hits initial target(s) on a line: arrow
        };

        private string[] _effectTypes =
        {
            "Elements","Death","Iron","Arrow","Spit", "Boulder"
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

        public Cell Origin
        {
            get { return _origin; }
            set { _origin = value; }
        }

        public Cell Target
        {
            get { return _target; }
            set { _target = value; }
        }

        public int Step
        {
            get { return _step; }
            set { _step = value; }
        }

        //public Instant(Cell source, Cell target, string shape, string effect, int radius=1)
        public Instant(string shape, string effect, int radius = 1)
        {

            _console = new RLConsole(Game.MapWidth, Game.MapHeight);
            _nullConsole = new RLConsole(30, Game.MapHeight);

            List<string> typesCheck = new List<string>(_projectileTypes);
            if (typesCheck.Contains(shape))
            {
                _projectileType = shape;

            }
            else
            {
                throw new ArgumentException($"Invalid instant shape {shape}.");
            }
            typesCheck = new List<string>(_effectTypes);
            if (typesCheck.Contains(effect))
            {
                _effect = effect;
            }
            else
            {
                throw new ArgumentException($"Invalid instant effect {effect}.");
            }
            _radius = radius;
            //_origin = source;
            //_target = target;
            _step = 0;
        }

        public override void DrawConsole()
        {
            // sometimes short range missiles are too slow
            if (_projectileType == "missile")
            {
                _totalSteps = TargetCells().Count() * _animateSteps;
            }

                DungeonMap dungeonMap = Game.DungeonMap;
            Player player = Game.Player;

            _console.Clear();
            dungeonMap.ComputeFov(player.X, player.Y, player.Awareness, true);
            dungeonMap.Draw(_console, _nullConsole);
            player.Draw(_console, dungeonMap);
            DrawEffect(_step);
            _step++;
        }

        public void DrawPatternOnTargetedCells(string colorPattern, int? whichcell = null, char? symbol = null)
        {

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
                // generate seemingly random color for each cell that will
                // remain constant for 8 draw consoles, using hand-coded
                // pseudo-RNG
                int cursorStepSize = _totalSteps / _animateSteps;
                int i = _step / cursorStepSize;
                int colorChoiceRNG = 1 + i * 101 + point.X * 11 + point.Y * 37 + (point.X * point.Y % 17);

                if (!dungeonMap.IsInFov(point.X, point.Y))
                {
                    continue;
                }
                if (colorPattern == "targeting")
                {
                    if (point.X == _target.X && point.Y == _target.Y)
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
                    switch (colorChoiceRNG % 3)
                    {
                        case 0:
                            highlightColor = Colors.Poisoncloud1;
                            break;
                        case 1:
                            highlightColor = Colors.Poisoncloud2;
                            break;
                        case 2:
                            highlightColor = Colors.Poisoncloud3;
                            break;
                    }
                }
                else if (colorPattern == "Elements")
                {
                    switch (colorChoiceRNG % 4)
                    {
                        case 0:
                            highlightColor = Swatch.DbDeepWater;
                            break;
                        case 1:
                            highlightColor = Colors.Gold;
                            break;
                        case 2:
                            highlightColor = Swatch.DbSky;
                            break;
                        case 3:
                            highlightColor = Swatch.DbBlood;
                            break;
                    }
                }
                else if (colorPattern == "Iron")
                {
                    highlightColor = Swatch.DbBrightMetal;
                }
                else if (colorPattern == "Spit")
                {
                    highlightColor = Swatch.DbVegetation;
                }
                else if (colorPattern == "Arrow")
                {
                    highlightColor = Swatch.DbMetal;
                }
                else if (colorPattern == "Boulder")
                {
                    highlightColor = Swatch.DbStone;
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

        public List<Cell> TargetCells()
        {
            DungeonMap dungeonMap = Game.DungeonMap;

            List<Cell> cellsTargeted = new List<Cell>();

            if (_projectileType == "line" || _projectileType == "missile")
            {
                cellsTargeted = dungeonMap.GetCellsAlongLine(_origin.X, _origin.Y, _target.X, _target.Y).ToList();
                // Contains origin cell, drop it.
                cellsTargeted.RemoveAt(0);
            }
            else if (_projectileType == "ball")
            {
                FieldOfView targetFOV = new FieldOfView(dungeonMap);
                targetFOV.ComputeFov(_target.X, _target.Y, _radius + 1, true);

                foreach (Cell cell in dungeonMap.GetCellsInRadius(_target.X, _target.Y, _radius))
                {
                    if (targetFOV.IsInFov(cell.X, cell.Y))
                    {
                        cellsTargeted.Add(cell);
                    }
                }
            }
            else if (_projectileType == "point")
            {
                cellsTargeted.Add(dungeonMap.GetCell(_target.X, _target.Y));
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

        // returns char symbol for long projectiles -- arrows, spears, etc
        // based on direction of missile path
        public char LengthyProjectileChar(int dxInt, int dyInt)
        {
            float dx = (float)dxInt;
            float dy = (float)dyInt;
            if (Math.Abs(dx) / Math.Abs(dy) > 1.5)
            {
                return '-';
            }
            else if (Math.Abs(dx) / Math.Abs(dy) < 0.66)
            {
                return '|';
            }
            else if ((dx > 0 && dy > 0) || (dx < 0 && dy < 0))
            {
                return '\\';
            }
            else
            {
                return '/';
            }
        }

        public void DrawEffect(int drawStep)
        {
            if (_projectileType == "ball" || _projectileType == "line" || _projectileType == "point")
            {
                DrawPatternOnTargetedCells(_effect);
            }
            else if (_projectileType == "missile")
            {
                Player player = Game.Player;
                int dx = _origin.X - _target.X;
                int dy = _origin.Y - _target.Y;
                char missileChar = LengthyProjectileChar(dx, dy);
                if (_effect == "Spit")
                {
                    missileChar = '*';
                }
                if (_effect == "Boulder")
                {
                    missileChar = 'o';
                }
                float pctComplete = (float)drawStep / (float)_totalSteps;
                int animateStep = (int)((float)(TargetCells().Count() - 1) * pctComplete + 0.5);
                DrawPatternOnTargetedCells(_effect, animateStep, missileChar);
            }
        }

        public bool DoEffectOnTarget()
        {
            StringBuilder attackMessage = new StringBuilder();
            if (_effect == "Elements")
            {
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
            else if (_effect == "Death")
            {
                foreach (Actor target in TargetActors())
                {
                    attackMessage.AppendFormat("{0} is immersed in poisonous vapors. ", target.Name);

                    if (target.IsUndead)
                    {
                        attackMessage.AppendFormat("{0} is unaffected by the poisonous vapors. ", target.Name);
                    }
                    else
                    {
                        int totalDamage = Dice.Roll("4d10");
                        int activationDamage = Dice.Roll("1d4"); // damage
                        int poisonSpeed = activationDamage * 2 + Dice.Roll("2d4"); // speed of activation
                        Poison poison = new Poison(target, totalDamage, poisonSpeed, activationDamage);
                    }
                }
            }
            else if (_effect == "Iron")
            {
                // figure out which Actor shot iron
                DungeonMap dungeonMap = Game.DungeonMap;
                Actor source;
                if (dungeonMap.GetMonsterAt(_origin.X, _origin.Y) != null)
                {
                    source = dungeonMap.GetMonsterAt(_origin.X, _origin.Y);
                }
                else
                {
                    source = Game.Player;
                    if (!(_origin.X == source.X && _origin.Y == source.Y))
                    {
                        throw new ArgumentException($"Iron requires Actor source. " +
                            $"No Actor found at origin ({_origin.X}, {_origin.Y}).");
                    }
                }

                int damage;
                foreach (Actor target in TargetActors())
                {
                    StringBuilder discardMessage = new StringBuilder();
                    damage = CommandSystem.ResolveArmor(target, source, Runes.BonusToDamageIron, false, discardMessage);
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
            else if (_effect == "Arrow")
            {
                // figure out which Actor shot
                DungeonMap dungeonMap = Game.DungeonMap;
                Actor source;
                if (dungeonMap.GetMonsterAt(_origin.X, _origin.Y) != null)
                {
                    source = dungeonMap.GetMonsterAt(_origin.X, _origin.Y);
                }
                else
                {
                    source = Game.Player;
                    if (!(_origin.X == source.X && _origin.Y == source.Y))
                    {
                        throw new ArgumentException($"Arrow requires Actor source. " +
                            $"No Actor found at origin ({_origin.X}, {_origin.Y}).");
                    }
                }

                Actor target = TargetActors().FirstOrDefault();
                if (target == null)
                {
                    return false;
                }

                // Attack does its own messaging
                CommandSystem.Attack(source, target, true);
            }

            if (!string.IsNullOrWhiteSpace(attackMessage.ToString()))
            {
                Game.MessageLog.Add(attackMessage.ToString());
            }
            return true;
        }

        public override bool ProcessInput(RLKeyPress rLKeyPress, RLMouse rLMouse)
        {
            if (rLMouse.GetLeftClick() || rLKeyPress != null)
            {
                // skip to end of animation
                _step = _totalSteps - 2;
            }
            
            if (_step < _totalSteps)
            {
                return false;
            }
            else
            {
                DoEffectOnTarget();
                return true;
            }
        }

    }
}
