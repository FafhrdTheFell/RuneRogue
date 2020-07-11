using System.Runtime.Hosting;
using System.Text;
using RogueSharp;
using RogueSharp.DiceNotation;
using RuneRogue.Core;
using RuneRogue.Interfaces;
using System;
using System.Runtime.ExceptionServices;

namespace RuneRogue.Systems
{
    public class CommandSystem
    {
        public bool IsPlayerTurn { get; set; }

        // Return value is true if the player was able to move
        // false when the player couldn't move, such as trying to move into a wall
        public bool MovePlayer(Direction direction)
        {
            int x = Game.Player.X;
            int y = Game.Player.Y;

            switch (direction)
            {
                case Direction.Up:
                    {
                        y = Game.Player.Y - 1;
                        break;
                    }
                case Direction.Down:
                    {
                        y = Game.Player.Y + 1;
                        break;
                    }
                case Direction.Left:
                    {
                        x = Game.Player.X - 1;
                        break;
                    }
                case Direction.Right:
                    {
                        x = Game.Player.X + 1;
                        break;
                    }
                case Direction.UpLeft:
                    {
                        x = Game.Player.X - 1;
                        y = Game.Player.Y - 1;
                        break;
                    }
                case Direction.UpRight:
                    {
                        x = Game.Player.X + 1;
                        y = Game.Player.Y - 1;
                        break;
                    }
                case Direction.DownLeft:
                    {
                        x = Game.Player.X - 1;
                        y = Game.Player.Y + 1;
                        break;
                    }
                case Direction.DownRight:
                    {
                        x = Game.Player.X + 1;
                        y = Game.Player.Y + 1;
                        break;
                    }
                default:
                    {
                        return false;
                    }
            }

            if (Game.DungeonMap.SetActorPosition(Game.Player, x, y))
            {
                return true;
            }

            Monster monster = Game.DungeonMap.GetMonsterAt(x, y);

            if (monster != null)
            {
                Attack(Game.Player, monster);
                return true;
            }

            return false;
        }

        public void EndPlayerTurn()
        {
            IsPlayerTurn = false;
        }

        public void ActivateMonsters()
        {
            IScheduleable scheduleable = Game.SchedulingSystem.Get();
            if (scheduleable is Player)
            {
                IsPlayerTurn = true;
                Game.SchedulingSystem.Add(Game.Player);
            }
            else if (scheduleable is Monster)
            {
                Monster monster = scheduleable as Monster;

                if (monster != null)
                {
                    monster.PerformAction(this);
                    Game.SchedulingSystem.Add(monster);
                }

                ActivateMonsters();
            }
        }

        public void MoveMonster(Monster monster, Cell cell)
        {
            if (!Game.DungeonMap.SetActorPosition(monster, cell.X, cell.Y))
            {
                if (Game.Player.X == cell.X && Game.Player.Y == cell.Y)
                {
                    Attack(monster, Game.Player);
                }
            }
        }

        public void Attack(Actor attacker, Actor defender)
        {
            StringBuilder attackMessage = new StringBuilder();
            StringBuilder defenseMessage = new StringBuilder();

            int hits = ResolveAttack(attacker, defender, attackMessage);

            //int blocks = ResolveDefense(defender, hits, attackMessage, defenseMessage);

            //Game.MessageLog.Add(attackMessage.ToString());
            

            //int damage = hits - blocks;

            int damage = 0;
            if (hits > 0)
            {
                damage = ResolveArmor(defender, attacker, attackMessage, defenseMessage);
            }
            if (!string.IsNullOrWhiteSpace(attackMessage.ToString()))
            {
                Game.MessageLog.Add(attackMessage.ToString());
            }

            ResolveDamage(attacker, defender, damage);
        }

        // The attacker rolls based on his stats to see if he gets any hits
        private static int ResolveAttack(Actor attacker, Actor defender, StringBuilder attackMessage)
        {
            int hits = 0;

            int diff = attacker.AttackSkill - defender.DefenseSkill;
            double chanceDouble = Math.Exp(0.11 * Convert.ToDouble(diff)) /
                (1 + Math.Exp(0.11 * Convert.ToDouble(diff)));
            double unadjustedChanceDouble = Math.Exp(0.11 * Convert.ToDouble(attacker.AttackSkill)) /
                (1 + Math.Exp(0.11 * Convert.ToDouble(attacker.AttackSkill)));
            int chanceInt = Convert.ToInt32(chanceDouble * 100 + 0.5);
            int unadjustedChanceInt = Convert.ToInt32(unadjustedChanceDouble * 100 + 0.5);
            int roll = Dice.Roll("1D100");
            if (roll > 101 - chanceInt)
            {
                attackMessage.AppendFormat("{0} attacks {1} and rolls {2}: {1} hits.", attacker.Name, defender.Name, roll);
                hits += 1;
            }
            else if (roll > 101 - unadjustedChanceInt)
            {
                attackMessage.AppendFormat("{0} attacks {1} and rolls {2}: {2} dodges.", attacker.Name, defender.Name, roll);
            }
            else
            {
                attackMessage.AppendFormat("{0} attacks {1} and rolls {2}: {1} misses.", attacker.Name, defender.Name, roll);
            }

            //Console.WriteLine(attacker.Name);
            //Console.WriteLine($"chance {chanceInt} D {defender.DefenseSkill} A {attacker.AttackSkill}");

            return hits;

        }

        private static int ResolveArmor(Actor defender, Actor attacker, StringBuilder attackMessage, StringBuilder defenseMessage)
        {
            string attackDice = "1d" + attacker.Attack.ToString();
            int attackResult = Dice.Roll(attackDice);
            if (attackResult <= defender.Defense && attackResult >= 4)
            {
                attackMessage.AppendFormat(" {0}'s blow bounces off {1}.", attacker.Name, defender.Name);
            }
            else if (attackResult <= defender.Defense)
            {
                attackMessage.AppendFormat(" {0}'s weak blow does no damage.", attacker.Name);
            }
            else if (attackResult >= defender.Defense * 2)
            {
                attackMessage.AppendFormat(" {0}'s blow lands hard!", attacker.Name);
            }

            return Math.Max(attackResult - defender.Defense, 0);

        }


        // Apply any damage that wasn't blocked to the defender
        private static void ResolveDamage(Actor attacker, Actor defender, int damage)
        {
            if (damage > 0)
            {
                defender.Health -= damage;
                if (defender.Health > 0)
                {
                    Game.MessageLog.Add($"  {defender.Name} takes {damage} damage");
                }
                if (defender.Health <= 0)
                {
                    Game.MessageLog.Add($"  {defender.Name} is killed");
                    ResolveDeath(attacker, defender);
                }
            }
        }

        // Remove the defender from the map and add some messages upon death.
        private static void ResolveDeath(Actor attacker, Actor defender)
        {
            if (defender is Player)
            {
                Game.MessageLog.Add($"  Game Over!");
                
            }
            else if (defender is Monster)
            {
                Game.DungeonMap.RemoveMonster((Monster)defender);

                Game.MessageLog.Add($"  {defender.Name} dropped {defender.Gold} gold");

                if (attacker is Player)
                {
                    attacker.Gold += defender.Gold;
                }
            }
        }
    }
}
