using System;
using System.Linq;
using RogueSharp;
using RogueSharp.DiceNotation;
using RuneRogue.Core;
using RuneRogue.Interfaces;
using RuneRogue.Systems;

namespace RuneRogue.Behaviors
{
    public class StandardMoveAndAttack : IBehavior
    {
        public bool Act(Monster monster, CommandSystem commandSystem)
        {
            DungeonMap dungeonMap = Game.DungeonMap;
            Player player = Game.Player;

            // Compute a field-of-view 
            // Use the monsters Awareness value for the distance in the FoV check
            // If the player is in the monster's FoV then alert it, unless the
            // player is invisible and passes a stealth test
            bool canSeePlayer = false;
            FieldOfView monsterFov = new FieldOfView(dungeonMap);
            monsterFov.ComputeFov(monster.X, monster.Y, monster.Awareness, true);
            //int distFromPlayer = Math.Abs(player.X - monster.X) + Math.Abs(player.Y - monster.Y);
            if (monsterFov.IsInFov(player.X, player.Y))
            {
                canSeePlayer = true;
                monster.LastLocationPlayerSeen = dungeonMap.GetCell(player.X, player.Y);
                if (player.SAStealthy)
                {
                    if (Game.CommandSystem.CheckStealth(player, monster))
                    {
                        // player unseen
                        canSeePlayer = false;
                        // check if monster extra confused and loses awareness of player's last location
                        if (Game.CommandSystem.CheckStealth(player, monster))
                        {
                            // monster.TurnsAlerted = null;
                            int dx = Dice.Roll("2d3k1") - Dice.Roll("2d3k1");
                            int dy = Dice.Roll("2d3k1") - Dice.Roll("2d3k1");
                            monster.LastLocationPlayerSeen = dungeonMap.GetCell(
                                monster.LastLocationPlayerSeen.X + dx, monster.LastLocationPlayerSeen.Y + dy);
                            if (monster.X == monster.LastLocationPlayerSeen.X && monster.Y == monster.LastLocationPlayerSeen.Y)
                            {
                                monster.TurnsAlerted = null;
                            }
                        }
                    }
                }
            }

            if (monster.SASenseThoughts)
            {
                if (monster.WithinDistance(player, RunesSystem.DistanceSenseThoughts))
                {
                    canSeePlayer = true;
                }
            }

            if (canSeePlayer)
            {
                // update memory
                monster.TurnsAlerted = 1;
                monster.LastLocationPlayerSeen = dungeonMap.GetCell(player.X, player.Y);
                // hide from player
                if (monster.SAStealthy)
                {
                    if (Game.CommandSystem.CheckStealth(monster, player))
                    {
                        monster.IsInvisible = true;
                        if (dungeonMap.MonstersInFOV().Contains(monster))
                        {
                            Game.MessageLog.Add($"{monster.Name} hides in the shadows.");

                        }
                    }
                    else if (monster.IsInvisible)
                    {
                        monster.IsInvisible = false;
                        if (dungeonMap.MonstersInFOV().Contains(monster))
                        {
                            Game.MessageLog.Add($"{player.Name} spots {monster.Name}.");
                        }
                    }

                }
            }

            if (monster.TurnsAlerted == null)
            {
                return true;
            }

            string action = null;

            // 50% chance to use special abilities
            if (Dice.Roll("1d100") < monster.ShootPropensity && canSeePlayer &&
                monster.WithinDistance(player, Math.Max(monster.SpecialAttackRange, monster.MissileRange)) &&
                dungeonMap.MissileNotBlocked(monster, player))
            {
                action = "shoot";
            }
            else if (Dice.Roll("1d100") < monster.WanderPropensity)
            {
                action = "wander";    
            }
            else
            {
                action = "advance";
            }

            bool actionResult;
            switch (action)
            {
                case "shoot":
                    actionResult = Shoot(monster, player, commandSystem);
                    break;
                case "wander":
                    actionResult = Wander(monster, commandSystem);
                    break;
                case "advance":
                    actionResult = Advance(monster, monster.LastLocationPlayerSeen, commandSystem);
                    break;
                default:
                    throw new Exception($"Invalid action {action}.");
            }



            // if monster reached last known player location, give up until
            // can see player again
            if (monster.X == monster.LastLocationPlayerSeen.X && monster.Y == monster.LastLocationPlayerSeen.Y)
            {
                monster.TurnsAlerted = null;
            }

            monster.TurnsAlerted++;

            // Lose alerted status 15 turns after losing sight of the player. 
            // As long as the player is still in FoV the monster will be realerted
            // Otherwise the monster will quit chasing the player.
            if (monster.TurnsAlerted > 15)
            {
                monster.TurnsAlerted = null;
            }

            return actionResult;
        }

        public bool Shoot(Monster monster, Actor target, CommandSystem commandSystem)
        {
            bool canUseMissile = monster.WithinDistance(target, monster.MissileRange);
            bool canUseSpecial = monster.WithinDistance(target, monster.SpecialAttackRange);

            bool yesAttackSpecial = canUseSpecial;
            if (canUseMissile && canUseSpecial)
            {
                // 33% chance to use missiles if both are available
                yesAttackSpecial = (Dice.Roll("1d3") == 1);
            }
            commandSystem.Shoot(monster, target, specialAttack: yesAttackSpecial);
            return true;
        }

        public bool Wander(Monster monster, CommandSystem commandSystem)
        {
            int dx = Dice.Roll("1d3") - 1;
            int dy = Dice.Roll("1d3") - 1;
            commandSystem.MoveMonster(monster, Game.DungeonMap.GetCell(monster.X + dx, monster.Y + dy));
            return true;
        }

        public bool Advance(Monster monster, Cell targetCell, CommandSystem commandSystem)
        {
            DungeonMap dungeonMap = Game.DungeonMap;
            Player player = Game.Player;

            // Before we find a path, make sure to make the monster and player Cells walkable
            dungeonMap.SetIsWalkable(monster.X, monster.Y, true);
            dungeonMap.SetIsWalkable(player.X, player.Y, true);

            PathFinder pathFinder = new PathFinder(dungeonMap);
            Path path = null;

            try
            {
                path = pathFinder.ShortestPath(
                   dungeonMap.GetCell(monster.X, monster.Y),
                   dungeonMap.GetCell(targetCell.X, targetCell.Y));

                // long paths are usually indirect. Avoid them if monster would fall asleep.
                if (path.Length > 15)
                {
                    path = null;
                }
            }
            catch (PathNotFoundException)
            {
                // The monster can see the player, but cannot find a path to him
                // This could be due to other monsters blocking the way
                // Add a message to the message log that the monster is waiting
                //Game.MessageLog.Add( $"{monster.Name} waits for a turn" );
            }

            // Don't forget to set the walkable status back to false
            dungeonMap.SetIsWalkable(monster.X, monster.Y, false);
            dungeonMap.SetIsWalkable(player.X, player.Y, false);



            // In the case that there was a path, tell the CommandSystem to move the monster
            if (path != null)
            {
                try
                {
                    // TODO: This should be path.StepForward() but there is a bug in RogueSharp V3
                    // The bug is that a path returned from the pathfinder does not include the source Cell
                    commandSystem.MoveMonster(monster, path.Steps.First());
                }
                catch (NoMoreStepsException)
                {
                    Game.MessageLog.Add($"{monster.Name} growls in frustration");
                }
            }
            else
            {
                // move monster towards player if there is space
                int dx = 1 * Convert.ToInt32(targetCell.X > monster.X)
                    - 1 * Convert.ToInt32(targetCell.X > player.X);
                int dy = 1 * Convert.ToInt32(targetCell.Y > monster.Y)
                    - 1 * Convert.ToInt32(targetCell.Y > player.Y);
                if (!(dx == 0) && !(dy == 0))
                {
                    // pick random direction
                    if (Dice.Roll("1d2") == 1)
                    {
                        dx = 0;
                    }
                    else
                    {
                        dy = 0;
                    }
                }
                commandSystem.MoveMonster(monster, dungeonMap.GetCell(monster.X + dx, monster.Y + dy));

            }
            return true;
        }

    }
}
