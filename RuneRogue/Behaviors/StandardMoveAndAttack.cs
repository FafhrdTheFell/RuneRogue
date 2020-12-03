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
            int distFromPlayer = Math.Abs(player.X - monster.X) + Math.Abs(player.Y - monster.Y);
            if (monsterFov.IsInFov(player.X, player.Y))
            {
                canSeePlayer = true;
                if (player.IsInvisible)
                {
                    string dieSize = (distFromPlayer + 3).ToString();
                    int stealthRoll = Dice.Roll("1d" + dieSize); // higher roll = more stealthy
                    if (stealthRoll > 2)
                    {
                        // player unseen
                        canSeePlayer = false;
                        //monster.TurnsAlerted = null;
                        if (stealthRoll > 4)
                        {
                            // lose awareness of player's last location
                            monster.LastLocationPlayerSeen = dungeonMap.GetCell(monster.X, monster.Y);
                        }
                    }
                }
            }

            if (canSeePlayer)
            {
                // update memory
                monster.TurnsAlerted = 1;
                monster.LastLocationPlayerSeen = dungeonMap.GetCell(player.X, player.Y);
            }

            if (canSeePlayer && monster.MissileRange >= distFromPlayer)
            {
                if (Dice.Roll("1d10") > 5)
                {
                    bool shotNotBlocked = true;
                    foreach (Cell cell in dungeonMap.GetCellsAlongLine(monster.X, monster.Y, player.X, player.Y))
                    {
                        if (cell.X == monster.X && cell.Y == monster.Y)
                        {
                            continue;
                        }
                        if (dungeonMap.GetMonsterAt(cell.X, cell.Y) != null)
                        {
                            shotNotBlocked = false;
                        }
                    }
                    if (shotNotBlocked)
                    {
                        commandSystem.Shoot(monster, player);
                        return true;
                    }
                }
            }
            else if (monster.MissileRange > 0)
            {
                // sometimes wait
                if (Dice.Roll("1d10") > 5)
                {
                    return true;
                }
            }

            if (monster.TurnsAlerted.HasValue)
            {

                // Before we find a path, make sure to make the monster and player Cells walkable
                dungeonMap.SetIsWalkable(monster.X, monster.Y, true);
                dungeonMap.SetIsWalkable(player.X, player.Y, true);

                PathFinder pathFinder = new PathFinder(dungeonMap);
                Path path = null;

                try
                {
                    path = pathFinder.ShortestPath(
                       dungeonMap.GetCell(monster.X, monster.Y),
                       dungeonMap.GetCell(monster.LastLocationPlayerSeen.X, monster.LastLocationPlayerSeen.Y));

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
                    int dx = 1 * Convert.ToInt32(monster.LastLocationPlayerSeen.X > monster.X) 
                        - 1 * Convert.ToInt32(monster.LastLocationPlayerSeen.X > player.X);
                    int dy = 1 * Convert.ToInt32(monster.LastLocationPlayerSeen.Y > monster.Y) 
                        - 1 * Convert.ToInt32(monster.LastLocationPlayerSeen.Y > player.Y);
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
            }
            return true;
        }
    }
}
