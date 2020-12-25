using System.Runtime.Hosting;
using System.Linq;
using System.Text;
using RogueSharp;
using RogueSharp.DiceNotation;
using RuneRogue.Core;
using RuneRogue.Interfaces;
using RuneRogue.Effects;
using RuneRogue.Items;
using System;
using System.Collections.Generic;

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

            // ideally would use Point code
            x += DirectionToCoordinates(direction)[0];
            y += DirectionToCoordinates(direction)[1];
            if (x == Game.Player.X && y == Game.Player.Y)
            {
                return false;
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

        public int[] DirectionToCoordinates(Direction direction)
        {
            int dx = 0;
            int dy = 0;

            switch (direction)
            {
                case Direction.Up:
                    {
                        dy -= 1;
                        break;
                    }
                case Direction.Down:
                    {
                        dy += 1;
                        break;
                    }
                case Direction.Left:
                    {
                        dx -= 1;
                        break;
                    }
                case Direction.Right:
                    {
                        dx += 1;
                        break;
                    }
                case Direction.UpLeft:
                    {
                        dx -= 1;
                        dy -= 1;
                        break;
                    }
                case Direction.UpRight:
                    {
                        dx += 1;
                        dy -= 1;
                        break;
                    }
                case Direction.DownLeft:
                    {
                        dx -= 1;
                        dy += 1;
                        break;
                    }
                case Direction.DownRight:
                    {
                        dx += 1;
                        dy += 1;
                        break;
                    }
            }
            return new int[]
            {
                dx,
                dy
            };
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

            PathFinder pathFinder = new PathFinder(dungeonMap.ExploredMap);
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

        public bool CloseDoorsNextTo(Actor actor)
        {
            DungeonMap dungeonMap = Game.DungeonMap;
            bool closedDoors = false;
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dy = -1; dy < 2; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }
                    else if (dungeonMap.GetDoor(actor.X+dx, actor.Y+dy) != null)
                    {
                        closedDoors = true;
                        dungeonMap.CloseDoor(actor, actor.X + dx, actor.Y + dy);
                    }
                }
            }
            return closedDoors;
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
                Game.RuneSystem.CheckDecayAllRunes();
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

                // activate next schedulable without returning to main loop:
                // makes game faster and only need main loop for player input
                if (!Game.SecondaryConsoleActive && Game.Player.Health > 0)
                {
                    ActivateMonsters();
                }
            }
            else if (scheduleable is Effect)
            {
                Effect effect = scheduleable as Effect;

                if (effect != null)
                {
                    // add effect before DoEffect in case DoEffect also stops effect
                    Game.SchedulingSystem.Add(effect);
                    effect.DoEffect();
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

        public bool CheckStealth(Actor sneak, Actor observer)
        {
            if (observer.SASenseThoughts)
            {
                return false;
            }
            int distance = (int)(Math.Pow((sneak.X - observer.X) * (sneak.X - observer.X) +
                (sneak.Y - observer.Y) * (sneak.Y - observer.Y), 0.5) + 0.5);
            string dieSize = (distance + 5).ToString();
            int stealthRoll = Dice.Roll("1d" + dieSize); // higher roll = more stealthy
            return (stealthRoll > 2) ? true : false;
        }

        public void Shoot(Actor attacker, Actor defender, bool specialAttack = false)
        {
            DungeonMap dungeonMap = Game.DungeonMap;
            Game.SecondaryConsoleActive = true;
            Game.AcceleratePlayer = false;
            string attackType = attacker.MissileType;
            if (specialAttack)
            {
                attackType = attacker.SpecialAttackType;
            }
            Instant shot = new Instant(attackType)
            {
                Source = attacker, //Origin = dungeonMap.GetCell(attacker.X, attacker.Y),
                Target = dungeonMap.GetCell(defender.X, defender.Y)
            };
            Game.CurrentSecondary = shot;
        }

        public static void WakeMonster(Actor actor)
        {
            if (actor is Monster)
            {
                Monster monster = actor as Monster;
                monster.TurnsAlerted = 1;
                monster.LastLocationPlayerSeen = Game.DungeonMap.GetCell(Game.Player.X, Game.Player.Y);
            }
        }

        public static void Attack(Actor attacker, Actor defender, bool missileAttack = false, 
            int hitBonus = 0, int damageBonus = 0)
        {
            StringBuilder attackMessage = new StringBuilder();

            if  (defender is Player)
            {
                Game.AutoMovePlayer = false;
            }

            bool attackHit = ResolveAttack(attacker, defender, attackMessage, missileAttack, 
                out bool isCritical, adjustment: hitBonus);

            int damage = 0;
            if (attackHit)
            {
                int damageTotalBonus = attacker.Attack * Convert.ToInt32(isCritical) / 2 + damageBonus;
                damage = ResolveArmor(defender, attacker, damageTotalBonus, missileAttack, attackMessage);
                if (defender == Game.Player && Game.XpOnAction)
                {
                    Game.Player.XpHealth += damage;
                }
            }

            ResolveDamage(attacker, defender, damage, missileAttack, attackMessage);

            if (defender.Health <= 0)
            {
                if (Game.AutoMoveMonsterTarget == defender)
                {
                    Game.AutoMoveMonsterTarget = null;
                    Game.AutoMovePlayer = false;
                }
                //ResolveDeath(attacker, defender, attackMessage);
            }

            if (!string.IsNullOrWhiteSpace(attackMessage.ToString()))
            {
                Game.MessageLog.Add(attackMessage.ToString());
            }

        }

        // The attacker rolls based on his stats to see if he gets any hits
        private static bool ResolveAttack(Actor attacker, Actor defender, StringBuilder attackMessage, 
            bool missileAttack, out bool critical, int adjustment = 0)
        {
            bool attackHit = false;
            // criticals only on backstab
            critical = false;
            int diff;
            // missile attacks use 1/2 defense skill
            if (missileAttack)
            {
                diff = attacker.AttackSkill - 3 - defender.DefenseSkill / 2;
            }
            else
            {
                diff = attacker.AttackSkill - defender.DefenseSkill;

            
                // ESP helps in melee combat against non-undead
                if (attacker.SASenseThoughts && !defender.IsUndead)
                {
                    diff += 4;
                }
                if (defender.SASenseThoughts && !attacker.IsUndead)
                {
                    diff -= 4;
                }
                diff += adjustment;
                if (attacker.SAStealthy && defender is Monster && !defender.SASenseThoughts)
                {
                    Monster monster = defender as Monster;
                    // if defender thinks attacker's location is not true location, critical
                    if (!(monster.LastLocationPlayerSeen.X == attacker.X && monster.LastLocationPlayerSeen.Y == attacker.Y))
                    {
                        attackMessage.AppendFormat("{1} ambushes {0}! ", monster.Name, attacker.Name);
                        diff += 6;
                        critical = true;
                    }
                }
                if (attacker.IsInvisible && defender is Player && !defender.SASenseThoughts)
                {
                    // 50% chance of critical
                    if (Dice.Roll("1d2") == 1)
                    {
                        attackMessage.AppendFormat("{1} ambushes {0}! ", Game.Player.Name, attacker.Name);
                        diff += 6;
                        critical = true;
                    }
                }
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
                if (defender.IsInvisible)
                {
                    defender.IsInvisible = false;
                    attackMessage.AppendFormat("{0} finds {1}. ", attacker.Name, defender.Name);
                }
                attackMessage.AppendFormat("{0} hits {1} (roll {2} < {3}).", attacker.Name, 
                    defender.Name, roll, chanceInt);
                attackHit = true;
                if (attacker.SALifedrainOnHit && !missileAttack)
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
                if (attacker.SADoppelganger && !missileAttack)
                {
                    // if symbol is @, then already has transformed
                    if (!(attacker.Symbol == '@'))
                    {
                        attackMessage.AppendFormat(" {0} transforms into {1}.", attacker.Name, defender.Name);
                        attacker.DoppelgangTransform();
                    }
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
            return attackHit;
        }

        public static int ResolveArmor(Actor defender, Actor attacker, int damageBonus, bool missileAttack, StringBuilder attackMessage)
        {
            string attackDice;
            int baseAttack;
            if (missileAttack)
            {
                baseAttack = attacker.MissileAttack + damageBonus;
            }
            else
            {
                baseAttack = attacker.Attack + damageBonus;
            }
            // HighImpact also applies to missile weapons
            if (attacker.SAHighImpact)
            {
                attackDice = "2d" + baseAttack.ToString() + "k1";
            }
            else
            {
                attackDice = "1d" + baseAttack.ToString();
            }
            int attackResult = Dice.Roll(attackDice);
            int defenseResult;
            if (defender.Defense > 0)
            {
                defenseResult = Dice.Roll("3d" + defender.Defense.ToString() + "k1");
            }
            else
            {
                defenseResult = 0;
            }
            if (attackResult <= defenseResult && attackResult >= 4)
            {
                attackMessage.AppendFormat(" The blow bounces off {1}'s armor.", attacker.Name, defender.Name);
            }
            else if (attackResult <= defenseResult)
            {
                attackMessage.AppendFormat(" The weak blow does no damage.", attacker.Name);
            }
            else if (attackResult >= defender.Defense * 2)
            {
                attackMessage.AppendFormat(" The blow lands hard!", attacker.Name);
            }

            return Math.Max(attackResult - defenseResult, 0);

        }


        // Apply any damage that wasn't blocked to the defender
        public static void ResolveDamage(Actor attacker, Actor defender, int damage, bool missileAttack, StringBuilder attackMessage)
        {
            if (damage > 0)
            {
                ResolveDamage(attacker.Name, defender, damage, missileAttack, attackMessage);

                if (attacker.SAVenomous && !missileAttack && defender.Health > 0)
                {
                    int poisonPotency = attacker.MaxHealth / 10 + 1;
                    Poison poison = new Poison(defender, poisonPotency);
                    attackMessage.AppendFormat(" {0}'s venom courses through {1}. ", attacker.Name, defender.Name);
                }
                if (attacker.SACausesStun && defender.Health > 0)
                {
                    // stun with 1/2 probability
                    if (Dice.Roll("1d10") < 6)
                    {
                        int stunPotency = attacker.MaxHealth / 15 + 1;
                        Stun stun = new Stun(defender, stunPotency);
                        attackMessage.AppendFormat(" {0}'s blow stuns {1}. ", attacker.Name, defender.Name);
                    }
                }
                if (attacker.SAVampiric && !missileAttack)
                {
                    int gain = damage;
                    attacker.Health = Math.Min(attacker.Health + gain, attacker.MaxHealth);
                    attackMessage.AppendFormat(" {0} feeds on {1}'s life.", attacker.Name, defender.Name);
                }
                if (attacker.SALifedrainOnDamage && !missileAttack && defender.Health > 0)
                {
                    int drain = Math.Max(damage / 2, 1);
                    defender.MaxHealth -= drain;
                    attackMessage.AppendFormat(" {0} feels cold.", defender.Name);
                    if (defender == Game.Player && Game.XpOnAction)
                    {
                        Game.Player.XpHealth += drain * 3;
                    }
                }
            }
        }

        public static void ResolveDamage(string damageSource, Actor defender, int damage, bool missileAttack, StringBuilder attackMessage)
        {
            if (damage > 0)
            {
                defender.Health -= damage;

                if (defender.Health > 0)
                {
                    attackMessage.AppendFormat(" {0} takes {1} damage. ", defender.Name, damage);
                    //Game.MessageLog.Add($"  {defender.Name} takes {damage} damage");
                }
                if (defender.Health <= 0 && defender.Health + damage > 0)
                {
                    attackMessage.AppendFormat(" {0} takes {1} damage, killing it. ", defender.Name, damage);
                    ResolveDeath(damageSource, defender, attackMessage);
                }
            }
        }

                // Remove the defender from the map and add some messages upon death.
        public static void ResolveDeath(string deathSource, Actor defender, StringBuilder attackMessage)
        {
            if (defender is Player)
            {
                //Game.MessageLog.Add($"{defender.Name} has died. Game Over! Final score: {Game.Player.LifetimeGold}.");
                if (Game.RuneSystem.RunesOwned().Contains("Life"))
                {
                    attackMessage.AppendFormat("{0}'s Rune of Life flashes! {0} is reborn whole. ", defender.Name);
                    defender.Health = defender.MaxHealth;
                    if (defender.ExistingEffect("poison") != null)
                    {
                        defender.ExistingEffect("poison").FinishEffect();
                    }
                    bool decayed = Game.RuneSystem.CheckDecay("Life", out List<string> messages);
                    if (decayed)
                    {
                        foreach (string s in messages)
                        {
                            attackMessage.Append(s + " ");
                        }
                    }
                }
                else
                {
                    attackMessage.Append($"{defender.Name} has died. Game over!");
                    Game.GameOver($"killed by {deathSource} on level " + Game.mapLevel.ToString());
                }
                
            }
            else if (defender is Monster)
            {
                if (defender.Gold > 0)
                {

                    if (Game.DungeonMap.GetItemAt(defender.X, defender.Y) is Gold)
                    {
                        Gold onground = (Gold)Game.DungeonMap.GetItemAt(defender.X, defender.Y);
                        onground.Amount += defender.Gold;
                    }
                    else
                    {
                        Gold gold = new Gold(defender.Gold)
                        {
                            X = defender.X,
                            Y = defender.Y,
                        };
                        Game.DungeonMap.AddItem(gold);
                    }
                }
                Game.DungeonMap.RemoveMonster((Monster)defender);
            }
        }
        public static void ResolveDeath(Actor attacker, Actor defender, StringBuilder attackMessage)
        {
            ResolveDeath(attacker.Name, defender, attackMessage);
        }
    }
}
