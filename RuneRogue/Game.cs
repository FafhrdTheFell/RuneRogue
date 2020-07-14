﻿using System;
using OpenTK.Graphics.ES20;
using OpenTK.Input;
using RLNET;
using RogueSharp.Random;
using RuneRogue.Core;
using RuneRogue.Systems;

namespace RuneRogue
{
    public static class Game
    {
        // The screen height and width are in number of tiles
        //private static readonly int _screenWidth = 100;
        private static readonly int _screenWidth = 120;
        private static readonly int _screenHeight = 70;
        private static RLRootConsole _rootConsole;

        // The map console takes up most of the screen and is where the map will be drawn
        //private static readonly int _mapWidth = 80;
        //private static readonly int _mapHeight = 48;
        private static readonly int _mapWidth = 100;
        private static readonly int _mapHeight = 56;
        private static RLConsole _mapConsole;
        private static RLConsole _secondaryConsole;

        // Below the map console is the message console which displays attack rolls and other information
        //private static readonly int _messageWidth = 80;
        //private static readonly int _messageHeight = 11;
        private static readonly int _messageWidth = 100;
        private static readonly int _messageHeight = 14;
        private static RLConsole _messageConsole;

        // The stat console is to the right of the map and display player and monster stats
        private static readonly int _statWidth = 20;
        private static readonly int _statHeight = 70;
        private static RLConsole _statConsole;

        // Above the map is the inventory console which shows the players equipment, abilities, and items
        private static readonly int _inventoryWidth = 80;
        private static readonly int _inventoryHeight = 11;
        private static RLConsole _inventoryConsole;

        private static InputSystem _inputSystem;
        private static bool _quittingGame;
        private static bool _triggerQuit;

        public static int mapLevel = 1;
        private static bool _renderRequired = true;

        public const int MaxDungeonLevel = 14;
        public const bool XpOnAction = false;
        public const int ShopEveryNLevels = 3;

        public static Player Player { get; set; }
        public static DungeonMap DungeonMap { get; private set; }
        public static MessageLog MessageLog { get; private set; }
        public static CommandSystem CommandSystem { get; private set; }
        public static SchedulingSystem SchedulingSystem { get; private set; }
        public static MonsterGenerator MonsterGenerator { get; private set; }

        public static Shop CurrentShop { get; set; } 

        public static bool AcceleratePlayer;
        public static bool SecondaryConsoleActive;

        private static RLKeyPress PrevKeyPress;

        // We can use this instance of IRandom throughout our game when generating random number
        public static IRandom Random { get; private set; }

        public static T RandomEnumValue<T>()
        {
            Array v = Enum.GetValues(typeof(T));
            return (T)v.GetValue(Random.Next(v.Length - 1));
        }

        public static object RandomArrayValue(Array array)
        {
            return array.GetValue(Random.Next(array.Length - 1));
        }

        public static void Main()
        {
            // Establish the seed for the random number generator from the current time
            int seed = (int)DateTime.UtcNow.Ticks;
            Random = new DotNetRandom(seed);

            // This must be the exact name of the bitmap font file we are using or it will error.
            // string fontFileName = "terminal8x8.png";
            string fontFileName = "Cheepicus_14x14.png";
            int fontHeight = 14;
            int fontWidth = 14;


            // The title will appear at the top of the console window along with the seed used to generate the level
            string consoleTitle = $"RuneRogue - Level {mapLevel} - Seed {seed}";

            // Create a new MessageLog and print the random seed used to generate the level
            MessageLog = new MessageLog();
            MessageLog.Add("The rogue arrives on level 1");
            MessageLog.Add($"Level created with seed '{seed}'");

            // Tell RLNet to use the bitmap font that we specified and that each tile is 8 x 8 pixels
            // _rootConsole = new RLRootConsole(fontFileName, _screenWidth, _screenHeight, 8, 8, 1f, consoleTitle);
            _rootConsole = new RLRootConsole(fontFileName, _screenWidth, _screenHeight, fontHeight, fontWidth, 1f, consoleTitle);
            _rootConsole.SetWindowState(RLWindowState.Maximized);

            // Initialize the sub consoles that we will Blit to the root console
            _mapConsole = new RLConsole(_mapWidth, _mapHeight);
            _secondaryConsole = new RLConsole(_mapWidth, _mapHeight);
            _messageConsole = new RLConsole(_messageWidth, _messageHeight);
            _statConsole = new RLConsole(_statWidth, _statHeight);
            _inventoryConsole = new RLConsole(_inventoryWidth, _inventoryHeight);

            CurrentShop = new Shop();

            AcceleratePlayer = false;
            PrevKeyPress = null;
            SecondaryConsoleActive = false;
            _inputSystem = new InputSystem();
            _quittingGame = false;
            _triggerQuit = false;

            SchedulingSystem = new SchedulingSystem();

            MonsterGenerator = new MonsterGenerator();
            MonsterGenerator.ReadMonsterData("Resources/Monsters.json");

            MapGenerator mapGenerator = new MapGenerator(_mapWidth, _mapHeight, 20, 13, 7, mapLevel);
            DungeonMap = mapGenerator.CreateMap();
            DungeonMap.UpdatePlayerFieldOfView();

            CommandSystem = new CommandSystem();

            // Set up a handler for RLNET's Update event
            _rootConsole.Update += OnRootConsoleUpdate;

            // Set up a handler for RLNET's Render event
            _rootConsole.Render += OnRootConsoleRender;

            // Set background color and text for each console so that we can verify they are in the correct positions
            _inventoryConsole.SetBackColor(0, 0, _inventoryWidth, _inventoryHeight, Swatch.DbWood);
            _inventoryConsole.Print(1, 1, "Inventory", Colors.TextHeading);
            _messageConsole.SetBackColor(0, 0, _messageWidth, _messageHeight, Swatch.DbDeepWater);
            _messageConsole.Print(1, 1, "Messages", Colors.TextHeading);

            //RLKeyPress keyPress = _rootConsole.Keyboard.GetKeyPress();


            // Begin RLNET's game loop
            _rootConsole.Run();
        }

        public static void QuitGame()
        {
            Game.MessageLog.Add($"Goodbye! Press any key to exit.");
            _triggerQuit = true;
        }

        // Event handler for RLNET's Update event
        private static void OnRootConsoleUpdate(object sender, UpdateEventArgs e)
        {
            bool didPlayerAct = false;
            RLKeyPress keyPress;

            if (SecondaryConsoleActive)
            {
                keyPress = _rootConsole.Keyboard.GetKeyPress();
                if (keyPress != null)
                {
                    AcceleratePlayer = false;
                    bool finished = CurrentShop.PurchaseChoice(keyPress);
                    if (finished)
                    {
                        SecondaryConsoleActive = false;
                    }
                    keyPress = null;
                    _renderRequired = true;
                }
            }

            if (AcceleratePlayer && DungeonMap.PlayerPeril)
            {
                AcceleratePlayer = false;
            }



            // if player requested acceleration, use the previous keypress,
            // but turn off acceleration if a key is pressed to stop it.
            if (AcceleratePlayer)
            {
                keyPress = _rootConsole.Keyboard.GetKeyPress();
                if (keyPress != null)
                {
                    AcceleratePlayer = false;
                    keyPress = null;
                }
                else
                {
                    keyPress = PrevKeyPress;
                }
            }
            else
            {
                keyPress = _rootConsole.Keyboard.GetKeyPress();
            }
            
            if (CommandSystem.IsPlayerTurn && keyPress != null)
            {
                Player.CheckAdvancement();
            }

            if (CommandSystem.IsPlayerTurn || _quittingGame)
            {
                if (keyPress != null)
                {
                    if (_quittingGame)
                    {
                        _rootConsole.Close();
                    }
                    else if (Game.Player.Health <= 0)
                    {
                        QuitGame();
                        _renderRequired = true;
                    }
                    else
                    {
                        Direction direction = _inputSystem.MoveDirection(keyPress);
                        if (direction != Direction.None)
                        {
                            // the acceleration system is ugly. AcceleratePlayer sometimes
                            // gets set in the Command System, so need to check shift before
                            // carrying out move.
                            AcceleratePlayer = _inputSystem.ShiftDown(keyPress);
                            didPlayerAct = CommandSystem.MovePlayer(direction);
                        }
                        else if (_inputSystem.QuitKeyPressed(keyPress))
                        {
                            QuitGame();
                            _renderRequired = true;
                        }
                        else if (_inputSystem.DescendStairs(keyPress))
                        {
                            if (DungeonMap.CanMoveDownToNextLevel())
                            {
                                // MapGenerator treats Game.MaxDungeonLevel differently
                                MapGenerator mapGenerator = new MapGenerator(_mapWidth, _mapHeight, 20, 13, 7, ++mapLevel);
                                DungeonMap = mapGenerator.CreateMap();
                                MessageLog = new MessageLog();
                                MessageLog.Add("The stairs collapse behind you.");
                                if (mapLevel == MaxDungeonLevel)
                                {
                                    MessageLog.Add("Are you ready to ascend the throne of runes?");
                                }
                                CommandSystem = new CommandSystem();
                                _rootConsole.Title = $"RuneRogue - Level {mapLevel}";
                                didPlayerAct = true;
                            }
                        }
                        else if (_inputSystem.WaitKey(keyPress))
                        {
                            didPlayerAct = true;
                        }
                    }
                    PrevKeyPress = keyPress;
                }

                if (didPlayerAct)
                {
                    _renderRequired = true;
                    CommandSystem.EndPlayerTurn();
                }
                // if player did not act and there was a keypress, it hit a wall
                // or something else, probably. Therefore stop acceleration loop.
                if (!(keyPress == null) && !(didPlayerAct))
                {
                    AcceleratePlayer = false;
                }

                _quittingGame = _triggerQuit;
            }
            else
            {
                CommandSystem.ActivateMonsters();
                _renderRequired = true;
            }
        }

        // Event handler for RLNET's Render event
        private static void OnRootConsoleRender(object sender, UpdateEventArgs e)
        {
            // Don't bother redrawing all of the consoles if nothing has changed.
            if (_renderRequired)
            {
                _mapConsole.Clear();
                _statConsole.Clear();
                _messageConsole.Clear();
                _messageConsole.SetBackColor(0, 0, _messageWidth, _messageHeight, Swatch.DbDeepWater);
                _statConsole.SetBackColor(0, 0, _statWidth, _statHeight, Swatch.DbOldStone);
                //_inventoryConsole.SetBackColor(0, 0, _inventoryWidth, _inventoryHeight, Swatch.DbWood);
                _secondaryConsole.SetBackColor(0, 0, _mapWidth, _mapHeight, Swatch.Compliment);

                if (!SecondaryConsoleActive)
                {
                    DungeonMap.Draw(_mapConsole, _statConsole);
                    Player.Draw(_mapConsole, DungeonMap);
                    Player.DrawStats(_statConsole);
                    MessageLog.Draw(_messageConsole);
                }
                else
                {
                    _secondaryConsole.Clear();
                    // need to draw DungeonMap to get monster stats
                    DungeonMap.Draw(_mapConsole, _statConsole);
                    Player.DrawStats(_statConsole);
                    MessageLog.Draw(_messageConsole);
                    CurrentShop.DrawConsole(_secondaryConsole);
                }

                // Blit the sub consoles to the root console in the correct locations
                //RLConsole.Blit(_mapConsole, 0, 0, _mapWidth, _mapHeight, _rootConsole, 0, _inventoryHeight);
                if (!SecondaryConsoleActive)
                {
                    RLConsole.Blit(_mapConsole, 0, 0, _mapWidth, _mapHeight, _rootConsole, 0, 0);
                }
                else
                {
                    RLConsole.Blit(_secondaryConsole, 0, 0, _mapWidth, _mapHeight, _rootConsole, 0, 0);
                }
                RLConsole.Blit(_messageConsole, 0, 0, _messageWidth, _messageHeight, _rootConsole, 0, _screenHeight - _messageHeight);
                RLConsole.Blit(_statConsole, 0, 0, _statWidth, _statHeight, _rootConsole, _mapWidth, 0);
                //RLConsole.Blit(_inventoryConsole, 0, 0, _inventoryWidth, _inventoryHeight, _rootConsole, 0, 0);

                // Tell RLNET to draw the console that we set
                _rootConsole.Draw();

                _renderRequired = false;
            }
        }
    }
}
