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
        private Actor _source;
        private Cell _origin;
        private Cell _target;
        private RLConsole _nullConsole;
        private string _projectileType;
        private string _effect;
        private int _radius;
        private int _step;
        private int _animateSteps = 5;
        private int _totalSteps = 36;
        // used to pass miscellaneous information, e.g., that rune decay should
        // be tested
        private string _specialOption; 

        private readonly string[] _projectileTypes =
        {
            "line", // hits everything on a line: death ray
            "ball", // explodes in a radius
            "point", // hits a particular cell
            "missile" // hits initial target(s) on a line: arrow
        };

        private readonly string[] _effectTypes =
        {
            "Arrow","Boulder","Death","Elements","Fire","Iron","Ram","Spit","Deathchant"
        };

        private readonly Dictionary<string, string> _projectile = new Dictionary<string, string>
        {
            ["Elements"] = "line",
            ["Death"] = "ball",
            ["Iron"] = "missile",
            ["Arrow"] = "missile",
            ["Spit"] = "missile",
            ["Boulder"] = "missile",
            ["Ram"] = "missile",
            ["Fire"] = "line",
            ["Deathchant"] = "ball"
        };

        private readonly Dictionary<string, RLColor[]> _colorPattern = new Dictionary<string, RLColor[]>
        {
            ["Arrow"] = new RLColor[]
            {
                Swatch.DbMetal
            },
            ["Boulder"] = new RLColor[]
            {
                Swatch.DbStone
            },
            ["Death"] = new RLColor[]
            { 
                Colors.Poisoncloud1,
                Colors.Poisoncloud2,
                Colors.Poisoncloud3
            },
            ["Deathchant"] = new RLColor[]
            {
                Swatch.Compliment,
                Swatch.ComplimentDarker,
                Swatch.ComplimentDarkest,
                Swatch.ComplimentLighter,
                Swatch.ComplimentLightest
            },
            ["Elements"] = new RLColor[]
            {
                Swatch.DbDeepWater,
                Colors.Gold,
                Swatch.DbSky,
                Swatch.DbBlood
            },
            ["Fire"] = new RLColor[]
            {
                Colors.Fire1,
                Colors.Fire2,
                Colors.Fire3,
                Colors.Fire4,
                Colors.Fire5
            },
            ["Iron"] = new RLColor[]
            {
                Swatch.DbBrightMetal
            },
            ["Ram"] = new RLColor[]
            {
                Swatch.ComplimentLightest
            },
            ["Spit"] = new RLColor[]
            {
                Swatch.DbVegetation
            }
        };

        private readonly List<string> _selfTargetingEffects = new List<string>
        {
            "Deathchant"
        };

        private readonly string[] _elements =
        {
            "fire",
            "lightning",
            "ice",
            "cold",
            "steam",
            "water",
            "air"
        };

        public Actor Source
        {
            get { return _source; }
            set { _source = value;
                _origin = Game.DungeonMap.GetCell(value.X, value.Y);
            }
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

        public string Effect
        {
            get { return _effect; }
            set { _effect = value; }
        }

        public override bool UsesTurn()
        {
            return (_specialOption=="Rune");
        }


        //public Instant(string shape, string effect, int radius = 1, string special = "")
        public Instant(string effect, int radius = 1, string special = "")
        {

            _console = new RLConsole(Game.MapWidth, Game.MapHeight);
            _nullConsole = new RLConsole(30, Game.MapHeight);

            //System.Console.WriteLine(effect);

            string shape = _projectile[effect];
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
            _specialOption = special;
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

            if (_selfTargetingEffects.Contains(_effect))
            {
                _target = _origin;
            }
            if (_effect == "Deathchant")
            {
                _radius = 8;
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

            List<Monster> monstersSeen = dungeonMap.MonstersInFOV(skipInvisible: false);

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
                int numColors = _colorPattern[_effect].Length;
                highlightColor = _colorPattern[_effect][colorChoiceRNG % numColors];

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
                if (_effect == "Ram")
                {
                    missileChar = Source.Symbol;
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
                        string element = (string)Game.RandomArrayValue(_elements);
                        attackMessage.AppendFormat("{0} is blasted by {1}.", target.Name, element);
                    }
                    damage = Dice.Roll("4d10");
                    CommandSystem.ResolveDamage("elemental blast", target, damage, false, attackMessage);
                    //    if (target.Health > 0)
                    //    {
                    //        CommandSystem.WakeMonster(target);
                    //        attackMessage.AppendFormat(" {0} takes {1} damage. ", target.Name, damage);
                    //    }
                    //    else
                    //    {
                    //        attackMessage.AppendFormat(" {0} takes {1} damage, killing it. ", target.Name, damage);
                    //        CommandSystem.ResolveDeath("elemental blast", target, attackMessage);
                    //        break;
                    //    }
                    //}
                }
            }
            if (_effect == "Fire")
            {
                int damage;
                foreach (Actor target in TargetActors())
                {
                    damage = Dice.Roll("2d10");
                    target.Health -= damage;

                    attackMessage.AppendFormat("Flames engulf {0}. ", target.Name);
                    CommandSystem.WakeMonster(target);
                    CommandSystem.ResolveDamage("fire", target, damage, false, attackMessage);
                    //if (target.Health > 0)
                    //{
                    //    CommandSystem.WakeMonster(target);
                    //    attackMessage.AppendFormat(" {0} takes {1} damage. ", target.Name, damage);
                    //}
                    //else
                    //{
                    //    attackMessage.AppendFormat(" {0} takes {1} damage, killing it. ", target.Name, damage);
                    //    CommandSystem.ResolveDeath("fire", target, attackMessage);
                    //    break;
                    //}
                   
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
                        CommandSystem.WakeMonster(target);
                        int potency = Dice.Roll("1d3") + 4;
                        Poison poison = new Poison(target, potency);
                    }
                }
            }
            else if (_effect == "Deathchant")
            {
                attackMessage.AppendFormat("{0} chants a deathly dirge. ", Source.Name);
                foreach (Actor target in TargetActors())
                {
                    CommandSystem.WakeMonster(target);
                    if (target.IsUndead)
                    {
                        if (target.Health < target.MaxHealth)
                        {
                            target.Health += 1;
                            attackMessage.AppendFormat("{0} reassembles. ", target.Name);
                        }
                    }
                    else if (target != Source)
                    {
                        CommandSystem.ResolveDamage("deathly dirge", target, 1, false, attackMessage);
                        //target.Health -= 1;

                        //if (target.Health > 0)
                        //{
                        //    attackMessage.AppendFormat(" {0} takes 1 damage. ", target.Name);
                        //}
                        //else
                        //{
                        //    attackMessage.AppendFormat(" {0} takes 1 damage, killing it. ", target.Name);
                        //    CommandSystem.ResolveDeath("deathly dirge", target, attackMessage);
                        //}                    
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
                    //target.Health -= damage;
                    CommandSystem.WakeMonster(target);

                    if (damage > 0)
                    {
                        attackMessage.AppendFormat("{0} is perforated. ", target.Name);
                        CommandSystem.ResolveDamage("razor-spear of iron", target, damage, false, attackMessage);
                    }
                    
                    
                        //if (target.Health > 0)
                        //{
                        //    CommandSystem.WakeMonster(target);
                        //    attackMessage.AppendFormat(" {0} takes {1} damage. ", target.Name, damage);
                        //}
                        //else
                        //{
                        //    attackMessage.AppendFormat(" {0} takes {1} damage, killing it. ", target.Name, damage);
                        //    CommandSystem.ResolveDeath("razor-spear of iron", target, attackMessage);
                        //}
                    //}
                    else
                    {
                        attackMessage.AppendFormat("The dart bounces off {0}'s armor.", target.Name);
                        break;
                    }
                }
            }
            else if (_effect == "Ram")
            {
                DungeonMap dungeonMap = Game.DungeonMap;
                Actor source = Source;

                int hitBonus = 0;
                int damageBonus = 0;
                if (_specialOption == "Rune")
                {
                    hitBonus = Runes.BonusToRamAttack;
                    damageBonus = Runes.BonusToRamAttack;
                }
                else if (source is Monster)
                {
                    Game.MessageLog.Add($"{source.Name} charges {TargetActor().Name}!");
                    hitBonus = 2 + source.AttackSkill / 4;
                    // set damageBonus to replace source.Attack damage with MissileAttack damage
                    damageBonus = source.Attack / 2;
                }

                // code if Ram-mer should stop at first target not destroyed
                //Cell newPosition = dungeonMap.GetCell(source.X, source.Y);
                //foreach (Cell cell in TargetCells())
                //{
                //    Actor target = source;
                //    if (dungeonMap.GetMonsterAt(cell.X, cell.Y) != null)
                //    {
                //        target = dungeonMap.GetMonsterAt(cell.X, cell.Y);
                //    }
                //    else if (Game.Player.X == cell.X && Game.Player.Y == cell.Y)
                //    {
                //        target = Game.Player;
                //    }
                //    if (target != source)
                //    {
                //        CommandSystem.Attack(source, target);
                //        // stop at defender that rammer attacked and failed to kill
                //        if (target.Health > 0)
                //        {
                //            break;
                //        }
                //    }
                //    else
                //    {
                //        newPosition = cell;
                //    }
                //    dungeonMap.SetActorPosition(source, newPosition.X, newPosition.Y);
                //}
                foreach (Actor target in TargetActors())
                {
                    StringBuilder discardMessage = new StringBuilder();
                    CommandSystem.Attack(source, target, hitBonus: hitBonus, damageBonus: damageBonus);
                }
                Cell newPosition = TargetCells().LastOrDefault(c => c.IsWalkable == true);
                if (newPosition != null)
                {
                    dungeonMap.SetActorPosition(source, newPosition.X, newPosition.Y);
                }
            }
            else if (_effect == "Arrow" || _effect == "Boulder" || _effect == "Spit")
            {
                // figure out which Actor shot
                DungeonMap dungeonMap = Game.DungeonMap;
                Actor source = Source;

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

        public override bool ProcessInput(RLKeyPress rLKeyPress, RLMouse rLMouse, out string message)
        {
            message = "";
            if (rLMouse.GetLeftClick() || rLKeyPress != null)
            {
                // skip to end of animation
                if (_step < _totalSteps - 1)
                {
                    _step = _totalSteps - 1;
                }
            }
            
            if (_step < _totalSteps)
            {
                return false;
            }
            else
            {
                DoEffectOnTarget();
                if (_specialOption == "Rune")
                Game.RuneSystem.CheckDecay(Effect);
                return true;
            }
        }


        public Actor TargetActor()
        {
            return ActorAtCell(_target);
        }

        public Actor ActorAtCell(Cell cell)
        {
            // figure out which Actor shot for arrow-type effects
            DungeonMap dungeonMap = Game.DungeonMap;
            Actor player = Game.Player;
            
            if (dungeonMap.GetMonsterAt(cell.X, cell.Y) != null)
            {
                return dungeonMap.GetMonsterAt(cell.X, cell.Y);
            }
            else if (player.X == cell.X && player.Y == cell.Y)
            {
                return player;
            }
            else
            {
                throw new ArgumentException($"Instant {_effect} requires Actor at cell. " +
                    $"No Actor found at ({_origin.X}, {_origin.Y}).");
            }
        }

    }
}
