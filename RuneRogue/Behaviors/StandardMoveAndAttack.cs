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

            bool canSeePlayer = false;

            FieldOfView monsterFov = new FieldOfView(dungeonMap);
            monsterFov.ComputeFov(monster.X, monster.Y, monster.Awareness, true);
            if (monsterFov.IsInFov(player.X, player.Y))
            {
                canSeePlayer = true;
                if (player.IsInvisible)
                {
                    int dist = Math.Abs(player.X - monster.X) + Math.Abs(player.Y - monster.Y);
                    string dieSize = (dist + 3).ToString();
                    if (Dice.Roll("1d" + dieSize) > 2)
                    {
                        canSeePlayer = false;
                        monster.TurnsAlerted = null;
                    }
                }
            }

            //monsterFov.ComputeFov(monster.X, monster.Y, monster.Awareness, true);

            // If the monster has not been alerted, compute a field-of-view 
            // Use the monsters Awareness value for the distance in the FoV check
            // If the player is in the monster's FoV then alert it
            // Add a message to the MessageLog regarding this alerted status
            if (!monster.TurnsAlerted.HasValue)
            {
                if (canSeePlayer)
                {
                    // Game.MessageLog.Add( $"{monster.Name} is eager to fight {player.Name}" );
                    monster.TurnsAlerted = 1;
                }
            }

            if (monster.TurnsAlerted.HasValue && canSeePlayer)
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
                       dungeonMap.GetCell(player.X, player.Y));
                    Console.WriteLine(monster.Name + " path");

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
                    int dx = 1 * Convert.ToInt32(player.X > monster.X) - 1 * Convert.ToInt32(monster.X > player.X);
                    int dy = 1 * Convert.ToInt32(player.Y > monster.Y) - 1 * Convert.ToInt32(monster.Y > player.Y);
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

                monster.TurnsAlerted++;

                // Lose alerted status every 15 turns. 
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
