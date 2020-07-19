using System.Runtime.Hosting;
using System.Linq;
using System.Text;
using RogueSharp;
using RogueSharp.DiceNotation;
using RuneRogue.Core;
using RuneRogue.Interfaces;
using RuneRogue.Items;
using System;
using System.Runtime.ExceptionServices;
using System.Security.Policy;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Threading;

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
 
            Shop shop = Game.DungeonMap.GetShopAt(x, y);

            if (shop != null)
            {
                EnterShop(Game.Player, shop);
                return true;
            }
            return false;
        }

        public bool AutoMovePlayer(int targetX, int targetY)
        {
            DungeonMap dungeonMap = Game.DungeonMap;
            Player player = Game.Player;
            if (targetX == player.X && targetY == player.Y)
            {
                return false;
            }
            // Before we find a path, make sure to make the valid destination and player Cells walkable
            dungeonMap.SetIsWalkable(player.X, player.Y, true);
            bool resetTargetWalkable = (dungeonMap.GetShopAt(targetX, targetY) != null || 
                dungeonMap.GetMonsterAt(targetX, targetY) != null);
            if (resetTargetWalkable)
            {
                dungeonMap.SetIsWalkable(targetX, targetY, true);
            }

            PathFinder pathFinder = new PathFinder(dungeonMap);
            Path path = null;

            try
            {
                path = pathFinder.ShortestPath(
                   dungeonMap.GetCell(player.X, player.Y),
                   dungeonMap.GetCell(targetX, targetY));
            }
            catch (PathNotFoundException)
            {
                // The monster can see the player, but cannot find a path to him
                // This could be due to other monsters blocking the way
                // Add a message to the message log that the monster is waiting
                Game.MessageLog.Add($"{player.Name} cannot find a way there.");
            }

            // Don't forget to set the walkable status back to false
            dungeonMap.SetIsWalkable(player.X, player.Y, false);
            if (resetTargetWalkable)
            {
                dungeonMap.SetIsWalkable(targetX, targetY, false);
            }

            // In the case that there was a path, tell the CommandSystem to move the monster
            if (path != null)
            {
                Cell firststep;
                int x = -1;
                int y = -1;
                try
                {
                    // TODO: This should be path.StepForward() but there is a bug in RogueSharp V3
                    // The bug is that a path returned from the pathfinder does not include the source Cell
                    firststep = path.Steps.First();
                    x = firststep.X;
                    y = firststep.Y;
                }
                catch (NoMoreStepsException)
                {
                    Game.MessageLog.Add($"{player.Name} growls in frustration");
                }
                if (x == -1 || y == -1)
                {
                    return false;
                }
                if (Game.DungeonMap.SetActorPosition(player, x, y))
                {
                    return true;
                }

                Monster monster = Game.DungeonMap.GetMonsterAt(x, y);

                if (monster != null)
                {
                    Attack(Game.Player, monster);
                    return true;
                }

                Shop shop = Game.DungeonMap.GetShopAt(x, y);

                if (shop != null)
                {
                    EnterShop(Game.Player, shop);
                    Game.AutoMovePlayer = false;
                    return true;
                }
                return false;

            }
            return false;
        }

        private void EnterShop(Actor actor, Shop shop)
        {
            //Shop shop = GetShop(x, y);
            if (shop != null && actor == Game.Player)
            {
                Game.SecondaryConsoleActive = true;
                Game.AcceleratePlayer = false;
                Game.CurrentSecondary = shop;
            }
        }

        public bool PickupItemPlayer()
        {
            Player player = Game.Player;
            Item item = Game.DungeonMap.GetItemAt(player.X, player.Y);
            if (item != null)
            {
                bool success = item.Pickup(player);
                if (success)
                {
                    Game.DungeonMap.RemoveItem(item);
                    return true;
                }
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

            // regeneration. player regens through rune only, which deactivates automatically.
            if (scheduleable is Actor)
            {
                Actor actor = scheduleable as Actor;
                if (actor.SARegeneration && actor.Health < actor.MaxHealth)
                {
                    int regained = Math.Min(Dice.Roll("4-2d3k1"), actor.MaxHealth - actor.Health);
                    actor.Health += regained;
                    if (actor is Player && actor.Health == actor.MaxHealth)
                    {
                        Game.RuneSystem.ToggleRune("Life");
                    }
                }
            }

            // rune decay
            if (scheduleable is Player)
            {
                Game.RuneSystem.CheckDecay();
            }

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
            else if (scheduleable is Effect)
            {
                Effect effect = scheduleable as Effect;

                if (effect != null)
                {
                    effect.DoEffect();
                    if (!effect.EffectFinished())
                    {
                        Game.SchedulingSystem.Add(effect);
                    }
                }
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

            if  (defender is Player)
            {
                Game.AutoMovePlayer = false;
            }

            int hits = ResolveAttack(attacker, defender, attackMessage);

            int damage = 0;
            if (hits > 0)
            {
                damage = ResolveArmor(defender, attacker, attackMessage);
                if (defender == Game.Player && Game.XpOnAction)
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
            if (attacker.SASenseThoughts && !defender.IsUndead)
            {
                diff += 5;
            }
            if (defender.SASenseThoughts && !attacker.IsUndead)
            {
                diff -= 5;
            }
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
                attackMessage.AppendFormat("{1} dodges {0}'s attack (roll {2} > {3}).", attacker.Name, 
                    defender.Name, roll, chanceInt);
            }
            else
            {
                attackMessage.AppendFormat("{0} misses {1} (roll {2} > {3}).", attacker.Name,
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
                    if (defender == Game.Player && Game.XpOnAction)
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
                if (defender.Gold > 0)
                {
                    attackMessage.AppendFormat(" {0} dropped some gold.", defender.Name);

                    if (Game.DungeonMap.GetItemAt(defender.X, defender.Y) is Gold)
                    {
                        Gold onground = (Gold)Game.DungeonMap.GetItemAt(defender.X, defender.Y);
                        onground.Amount += defender.Gold;
                    }
                    else
                    {
                        Gold gold = new Gold()
                        {
                            Amount = defender.Gold,
                            X = defender.X,
                            Y = defender.Y,
                        };
                        Game.DungeonMap.AddItem(gold);
                    }
                }
                Game.DungeonMap.RemoveMonster((Monster)defender);
            }


        }
    }
}
