using System.Runtime.Hosting;
using System.Text;
using RogueSharp;
using RogueSharp.DiceNotation;
using RuneRogue.Core;
using RuneRogue.Interfaces;
using System;
using System.Runtime.ExceptionServices;
using System.Security.Policy;

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
                    if (monster.SARegeneration && monster.Health < monster.MaxHealth)
                    {
                        monster.Health += Dice.Roll("4-2d3k1");
                    }
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

            int hits = ResolveAttack(attacker, defender, attackMessage);

            int damage = 0;
            if (hits > 0)
            {
                damage = ResolveArmor(defender, attacker, attackMessage);
                if (defender == Game.Player)
                {
                    Game.Player.XpHealth += damage;
                }
            }

            ResolveDamage(attacker, defender, damage, attackMessage);

            if (defender.Health <= 0)
            {
                ResolveDeath(attacker, defender, attackMessage);
            }

            if (!string.IsNullOrWhiteSpace(attackMessage.ToString()))
            {
                Game.MessageLog.Add(attackMessage.ToString());
            }

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
            int chanceInt = Convert.ToInt32(chanceDouble * 100 + 0.5) + 1;
            int unadjustedChanceInt = Convert.ToInt32(unadjustedChanceDouble * 100 + 0.5) + 1;
            int roll = Dice.Roll("1D100");
            if (roll < chanceInt)
            {
                attackMessage.AppendFormat("{0} hits {1} (roll {2} < {3}).", attacker.Name, 
                    defender.Name, roll, chanceInt);
                hits += 1;
                if (attacker.SALifedrainOnHit)
                {
                    if (defender.Health == defender.MaxHealth)
                    {
                        defender.Health--;
                        defender.MaxHealth--;
                    }
                    else
                    {
                        defender.MaxHealth--;
                    }
                    if (defender == Game.Player && Game.XpOnAction)
                    {
                        Game.Player.XpHealth += 3;
                    }
                    attackMessage.AppendFormat(" {0} feels cold.", defender.Name);
                }
                if (attacker.SADoppelganger)
                {
                    attacker.DoppelgangTransform();
                    attackMessage.AppendFormat(" {0} transforms into {1}.", attacker.Name, defender.Name);
                }
                // Player gets attack XP on hit
                if (attacker == Game.Player && Game.XpOnAction)
                {
                    Game.Player.XpAttackSkill += Math.Max(defender.DefenseSkill - attacker.AttackSkill, 1);
                    Game.Player.XpHealth += 1;
                }
            }
            else if (roll < unadjustedChanceInt)
            {
                attackMessage.AppendFormat("{1} dodges {0}'s attack (roll {2} < {3}).", attacker.Name, 
                    defender.Name, roll, chanceInt);
            }
            else
            {
                attackMessage.AppendFormat("{0} misses {1} (roll {2} < {3}).", attacker.Name,
                    defender.Name, roll, chanceInt);
            }
            if (roll < unadjustedChanceInt && defender == Game.Player && Game.XpOnAction)
            {
                Game.Player.XpDefenseSkill += Math.Max(attacker.AttackSkill - defender.DefenseSkill, 1);
                Game.Player.XpHealth += 1;

            }

            return hits;

        }

        private static int ResolveArmor(Actor defender, Actor attacker, StringBuilder attackMessage)
        {
            string attackDice;
            if (!attacker.SAHighImpact)
            {
                attackDice = "1d" + attacker.Attack.ToString();
            }
            else
            {
                attackDice = "2d" + attacker.Attack.ToString() + "k1";
            }
            int attackResult = Dice.Roll(attackDice);
            if (attackResult <= defender.Defense && attackResult >= 4)
            {
                attackMessage.AppendFormat(" The blow bounces off {1}'s armor.", attacker.Name, defender.Name);
            }
            else if (attackResult <= defender.Defense)
            {
                attackMessage.AppendFormat(" The weak blow does no damage.", attacker.Name);
            }
            else if (attackResult >= defender.Defense * 2)
            {
                attackMessage.AppendFormat(" The blow lands hard!", attacker.Name);
            }

            return Math.Max(attackResult - defender.Defense, 0);

        }


        // Apply any damage that wasn't blocked to the defender
        private static void ResolveDamage(Actor attacker, Actor defender, int damage, StringBuilder attackMessage)
        {
            if (damage > 0)
            {
                defender.Health -= damage;
                if (attacker.SAVampiric)
                {
                    int gain = damage;
                    attacker.Health = Math.Min(attacker.Health + gain, attacker.MaxHealth);
                    attackMessage.AppendFormat(" {0} feeds on {1}'s life.", attacker.Name, defender.Name);
                }
                if (damage > 0 && attacker.SALifedrainOnDamage)
                {
                    int drain = Math.Max(damage / 2, 1);
                    defender.MaxHealth -= drain;
                    attackMessage.AppendFormat(" {0} feels cold.", defender.Name);
                    if (defender == Game.Player)
                    {
                        Game.Player.XpHealth += drain * 3;
                    }
                }
                if (defender.Health > 0)
                {
                    attackMessage.AppendFormat(" {0} takes {1} damage.", defender.Name, damage);
                    //Game.MessageLog.Add($"  {defender.Name} takes {damage} damage");
                }
                if (defender.Health <= 0)
                {
                    attackMessage.AppendFormat(" {0} kills {1} ({2} damage).", attacker.Name, defender.Name, damage);
                }
            }
        }

        // Remove the defender from the map and add some messages upon death.
        private static void ResolveDeath(Actor attacker, Actor defender, StringBuilder attackMessage)
        {
            if (defender is Player)
            {
                //Game.MessageLog.Add($"{defender.Name} has died. Game Over! Final score: {Game.Player.LifetimeGold}.");
                attackMessage.AppendFormat(" {0} has died. Game over! Final score {1}.", defender.Name, Game.Player.LifetimeGold);


            }
            else if (defender is Monster)
            {
                Game.DungeonMap.RemoveMonster((Monster)defender);

                if (defender.Gold > 0)
                {
                    //Game.MessageLog.Add($"  {defender.Name} dropped {defender.Gold} gold.");
                    attackMessage.AppendFormat(" {0} dropped {1} gold.", defender.Name, defender.Gold);
                }
                

                if (attacker is Player)
                {
                    attacker.Gold += defender.Gold;
                }
            }
        }
    }
}
