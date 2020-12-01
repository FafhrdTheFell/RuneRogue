﻿using Microsoft.SqlServer.Server;
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
        }

        public override void DrawConsole()
        {
            DungeonMap dungeonMap = Game.DungeonMap;
            Player player = Game.Player;

            _console.Clear();
            dungeonMap.ComputeFov(player.X, player.Y, player.Awareness, true);
            dungeonMap.Draw(_console, _nullConsole);
            player.Draw(_console, dungeonMap);
            DrawEffect();
            DoEffectOnTarget();
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

        public void DrawEffect()
        {
            if (_effect == "Elements")
            {
                for (int i = 0; i < 8; i++)
                {
                    DrawPatternOnTargetedCells(_effect);
                    for (int j = 0; j < 10; j++)
                    {
                        Game.DrawRoot(this.Console);
                    }
                }
            }
            else if (_effect == "Death")
            {
                // not sure how to do the graphics timing better than these loops
                for (int i = 0; i < 8; i++)
                {
                    DrawPatternOnTargetedCells(_effect);
                    for (int j = 0; j < 10; j++)
                    {
                        Game.DrawRoot(this.Console);
                    }
                }
            }
            else if (_effect == "Iron")
            {
                Player player = Game.Player;
                int dx = _origin.X - _target.X;
                int dy = _origin.Y - _target.Y;
                char missileChar = LengthyProjectileChar(dx, dy);
                for (int i = 0; i < TargetCells().Count; i++)
                {
                    DrawPatternOnTargetedCells(_effect, i, missileChar);
                    for (int j = 0; j < 10; j++)
                    {
                        Game.DrawRoot(this.Console);
                    }
                }
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
            if (_effect == "Death")
            {
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
                        throw new ArgumentException($"Iron requires Actor source. No Actor found at origin ({_origin.X}, {_origin.Y}).");
                    }
                }
                
                int damage;
                foreach (Actor target in TargetActors())
                {
                    StringBuilder discardMessage = new StringBuilder();
                    damage = CommandSystem.ResolveArmor(target, source, Runes.BonusToDamageIron, discardMessage);
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

        public override bool ProcessInput(RLKeyPress rLKeyPress, RLMouse rLMouse)
        {
            return true;
        }

    }
}
