using System;
using OpenTK.Graphics.ES20;
using OpenTK.Input;
using RLNET;
using RogueSharp;
using RogueSharp.Random;
using RuneRogue.Core;
using RuneRogue.Effects;
using RuneRogue.Systems;
using RuneRogue.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace RuneRogue
{
    public static class Game
    {
        // The screen height and width are in number of tiles
        //private static readonly int _screenWidth = 100;
        private static int _screenWidth = 120;
        private static int _screenHeight = 70;
        private static RLRootConsole _rootConsole;

        // The map console takes up most of the screen and is where the map will be drawn
        //private static readonly int _mapWidth = 80;
        //private static readonly int _mapHeight = 48;
        private static int _mapWidth = 100;
        private static int _mapHeight = 56;
        private static RLConsole _mapConsole;

        // Below the map console is the message console which displays attack rolls and other information
        //private static readonly int _messageWidth = 80;
        //private static readonly int _messageHeight = 11;
        private static int _messageWidth = 100;
        private static int _messageHeight = 14;
        private static RLConsole _messageConsole;

        // The stat console is to the right of the map and display player and monster stats
        private static int _statWidth = 22;
        private static int _statHeight = 70;
        private static RLConsole _statConsole;

        private static InputSystem _inputSystem;

        private static string _playerFate;
        private static bool _gameOver;
        private static bool _quittingGame;
        private static string _highScoreFile = "scores.txt";
        private static string _nameFile = "prior_name.txt";

        public static int mapLevel = 1;
        private static bool _renderRequired = true;

        public const int MaxDungeonLevel = 14;
        public const bool XpOnAction = false;
        public const int ShopEveryNLevels = 3;
        public const int RuneForgeEveryNLevels = 4;

        public static Player Player { get; set; }
        public static DungeonMap DungeonMap { get; private set; }
        public static MessageLog MessageLog { get; private set; }
        public static CommandSystem CommandSystem { get; private set; }
        public static SchedulingSystem SchedulingSystem { get; private set; }
        public static MonsterGenerator MonsterGenerator { get; private set; }
        public static Runes RuneSystem { get; private set; }
        public static SecondaryConsole CurrentSecondary { get; set; }
        public static SecondaryConsole PostSecondary { get; set; }           
        public static TargetingSystem TargetingSystem { get; set; }
        public static string HighScoreFile
        {
            get { return "Resources/" + _highScoreFile; }
        }
        public static string NameFile
        {
            get { return "Resources/" + _nameFile; }
        }

        public static int MessageWidth
        {
            get { return _messageWidth; }
        }

        public static int MessageLines
        {
            get { return _messageHeight; }
        }

        public static int MapHeight
        {
            get { return _mapHeight; }
        }

        public static int MapWidth
        {
            get { return _mapWidth; }
        }

        public static int StatWidth
        {
            get { return _statWidth; }
        }

        // accelerate player continues move in straight line automatically
        // automoveplayer moves to selected point using pathfinding
        public static bool AcceleratePlayer;
        public static bool AutoMovePlayer;
        private static int AutoMoveXTarget;
        private static int AutoMoveYTarget;
        public static Monster AutoMoveMonsterTarget;

        public static bool SecondaryConsoleActive;

        //private static RLKeyPress PrevKeyPress;
        private static Direction AccelerateDirection;

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
            //string fontFileName = "Cheepicus_14x14.png";
            //int fontHeight = 14;
            //int fontWidth = 14;
            string fontFileName = "Resources/Talryth_square_15x15.png";
            int fontHeight = 15;
            int fontWidth = 15;


            // The title will appear at the top of the console window along with the seed used to generate the level
            string consoleTitle = $"RuneRogue - Level {mapLevel} - Seed {seed}";

            // Create a new MessageLog and print the random seed used to generate the level
            MessageLog = new MessageLog();
            MessageLog.Add("The rogue arrives on level 1");
            MessageLog.Add($"Level created with seed '{seed}'");

            // Tell RLNet to use the bitmap font that we specified and that each tile is 8 x 8 pixels
            // _rootConsole = new RLRootConsole(fontFileName, _screenWidth, _screenHeight, 8, 8, 1f, consoleTitle);
            RLSettings rLSettings = new RLSettings
            {
                //StartWindowState = RLWindowState.Fullscreen,
                StartWindowState = RLWindowState.Maximized,
                Height = _screenHeight,
                Width = _screenWidth,
                CharHeight = fontHeight,
                CharWidth = fontWidth,
                ResizeType = RLResizeType.ResizeCells,
                Title = consoleTitle,
                BitmapFile = fontFileName
            };
            //_rootConsole = new RLRootConsole(fontFileName, _screenWidth, _screenHeight, fontHeight, fontWidth, 1f, consoleTitle);
            _rootConsole = new RLRootConsole(rLSettings);
            //_rootConsole.SetWindowState(RLWindowState.Fullscreen);

            _screenWidth = _rootConsole.Width;
            _screenHeight = _rootConsole.Height;
            _mapHeight = _rootConsole.Height - _messageHeight;
            _mapWidth = _rootConsole.Width - _statWidth;
            _messageWidth = _screenWidth;
            _statHeight = _rootConsole.Height;

            // Initialize the sub consoles that we will Blit to the root console
            _mapConsole = new RLConsole(_mapWidth, _mapHeight);
            _messageConsole = new RLConsole(_messageWidth, _messageHeight);
            _statConsole = new RLConsole(_statWidth, _statHeight);

            AutoMovePlayer = false;
            AcceleratePlayer = false;
            
            _inputSystem = new InputSystem();

            _gameOver = false;
            _quittingGame = false;

            SchedulingSystem = new SchedulingSystem();

            MonsterGenerator = new MonsterGenerator();
            MonsterGenerator.ReadMonsterData("Resources/Monsters.json");

            RuneSystem = new Runes();

            InputConsole NameInput = new InputConsole();

            CurrentSecondary = NameInput;
            SecondaryConsoleActive = true;

            MapGenerator mapGenerator = new MapGenerator(_mapWidth, _mapHeight, 20, 13, 7, mapLevel);
            DungeonMap = mapGenerator.CreateMap();
            DungeonMap.UpdatePlayerFieldOfView();

            CommandSystem = new CommandSystem();

            //TargetingSystem = new TargetingSystem();


            // Set up a handler for RLNET's Update event
            _rootConsole.Update += OnRootConsoleUpdate;

            // Set up a handler for RLNET's Render event
            _rootConsole.Render += OnRootConsoleRender;

            // Set background color and text for each console so that we can verify they are in the correct positions
            _messageConsole.SetBackColor(0, 0, _messageWidth, _messageHeight, Swatch.DbDeepWater);
            _messageConsole.Print(1, 1, "Messages", Colors.TextHeading);

            //RLKeyPress keyPress = _rootConsole.Keyboard.GetKeyPress();

            //Stun stun = new Stun(Player, 3);
            //Poison poison = new Poison(Player,10,6,3);
            //bool isP = Player.ExistingEffect("poison") != null;
            //System.Console.WriteLine(isP);

            // Begin RLNET's game loop at 45 fps
            _rootConsole.Run(45);
        }

        public static bool FinalLevel()
        {
            return MaxDungeonLevel == mapLevel;
        }

        // Event handler for RLNET's Update event
        private static void OnRootConsoleUpdate(object sender, UpdateEventArgs e)
        {
            bool didPlayerAct = false;
            RLKeyPress keyPress = _rootConsole.Keyboard.GetKeyPress();
            RLMouse rLMouse = _rootConsole.Mouse;

            if (SecondaryConsoleActive)
            {
                AcceleratePlayer = false;
                string completionMessage;
                bool finished = CurrentSecondary.ProcessInput(keyPress, rLMouse, out completionMessage);
                if (finished)
                {
                    SecondaryConsoleActive = false;
                    if (completionMessage != "Cancelled" && !(CurrentSecondary is InputConsole))
                    {
                        if (CurrentSecondary is TargetingSystem)
                        {
                            if (completionMessage != "travel")
                            {
                                CurrentSecondary = PostSecondary;
                                SecondaryConsoleActive = true;
                            }
                            else
                            {
                                AutoMovePlayer = true;
                                TargetingSystem targetingSystem = CurrentSecondary as TargetingSystem;
                                AutoMoveXTarget = targetingSystem.Target.X;
                                AutoMoveYTarget = targetingSystem.Target.Y;
                                if (!DungeonMap.GetCell(AutoMoveXTarget, AutoMoveYTarget).IsExplored)
                                {
                                    Cell target = DungeonMap.GetNearestObject("explorable", true,
                            targetCell: DungeonMap.GetCell(AutoMoveXTarget, AutoMoveYTarget));
                                    if (target != null)
                                    {
                                        AutoMoveXTarget = target.X;
                                        AutoMoveYTarget = target.Y;
                                    }
                                }
                                return;
                            }
                        }
                        else if (CurrentSecondary is Instant)
                        {
                            if (CurrentSecondary.UsesTurn())
                            {
                                didPlayerAct = true;
                            }
                        }
                    }
                    if (CurrentSecondary is InputConsole)
                    {
                        Game.Player.Name = completionMessage;
                    }
                }
                _renderRequired = true;
            }

            if (_quittingGame)
            {
                if (keyPress != null || rLMouse.GetLeftClick() || rLMouse.GetRightClick())
                {
                    _rootConsole.Close();
                }

            }

            if (CommandSystem.IsPlayerTurn)
            {
                if (DungeonMap.PlayerPeril)
                {
                    AcceleratePlayer = false;
                    AutoMovePlayer = false;
                }

                if (!didPlayerAct && !SecondaryConsoleActive)
                {
                    didPlayerAct = AutoActionAndProcessInput(keyPress, rLMouse);
                }
                
                if (CommandSystem.IsPlayerTurn && didPlayerAct && Game.XpOnAction)
                {
                    Player.CheckAdvancementXP();
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

            }
            else if (Player.Health > 0)
            {
                CommandSystem.ActivateMonsters();
                _renderRequired = true;
            }
            if (_gameOver && !_quittingGame)
            {
                NewScore();
            }
        }

        public static void DrawRoot(RLConsole drawnConsole)
        {
            RLConsole.Blit(drawnConsole, 0, 0, _mapWidth, _mapHeight, _rootConsole, 0, 0);
            _rootConsole.Draw();
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

                if (!SecondaryConsoleActive)
                {
                    DungeonMap.Draw(_mapConsole, _statConsole);
                    Player.Draw(_mapConsole, DungeonMap);
                    Player.DrawStats(_statConsole);
                    MessageLog.Draw(_messageConsole);
                }
                else
                {
                    // need to draw DungeonMap to get monster stats
                    DungeonMap.Draw(_mapConsole, _statConsole);
                    Player.Draw(_mapConsole, DungeonMap);
                    Player.DrawStats(_statConsole);
                    MessageLog.Draw(_messageConsole);
                    CurrentSecondary.DrawConsole();
                }

                // Blit the sub consoles to the root console in the correct locations
                //RLConsole.Blit(_mapConsole, 0, 0, _mapWidth, _mapHeight, _rootConsole, 0, _inventoryHeight);
                if (!SecondaryConsoleActive)
                {
                    RLConsole.Blit(_mapConsole, 0, 0, _mapWidth, _mapHeight, _rootConsole, 0, 0);
                    RLConsole.Blit(_statConsole, 0, 0, _statWidth, _statHeight, _rootConsole, _mapWidth, 0);
                }
                else
                {
                    RLConsole.Blit(CurrentSecondary.Console, 0, 0, _mapWidth, _mapHeight, _rootConsole, 0, 0);
                    if (CurrentSecondary is TargetingSystem)
                    {
                        TargetingSystem targeting = CurrentSecondary as TargetingSystem;
                        RLConsole.Blit(targeting.StatConsole, 0, 0, _statWidth, _statHeight, _rootConsole, _mapWidth, 0);
                    }
                    else
                    {
                        RLConsole.Blit(_statConsole, 0, 0, _statWidth, _statHeight, _rootConsole, _mapWidth, 0);
                    }
                }
                RLConsole.Blit(_messageConsole, 0, 0, _messageWidth, _messageHeight, _rootConsole, 0, _screenHeight - _messageHeight);
                

                // Tell RLNET to draw the console that we set
                _rootConsole.Draw();

                _renderRequired = false;
            }
        }

        // execute automatic actions -- automove, autopickup -- or execute action from
        // keyboard or mouse input
        public static bool AutoActionAndProcessInput(RLKeyPress keyPress, RLMouse rLMouse)
        {
            bool didPlayerAct = false;
            // autopickup
            if (DungeonMap.GetItemAt(Player.X, Player.Y) != null && DungeonMap.MonstersInFOV().Count == 0)
            {
                return CommandSystem.PickupItemPlayer();
            }
            if (AutoMovePlayer)
            {
                if (AutoMoveMonsterTarget != null)
                {
                    AutoMoveXTarget = AutoMoveMonsterTarget.X;
                    AutoMoveYTarget = AutoMoveMonsterTarget.Y;
                }
                didPlayerAct = CommandSystem.AutoMovePlayer(AutoMoveXTarget, AutoMoveYTarget);
                if (!didPlayerAct)
                {
                    if (Player.X == AutoMoveXTarget && Player.Y == AutoMoveYTarget &&
                        DungeonMap.GetItemAt(AutoMoveXTarget, AutoMoveYTarget) != null)
                    {
                        didPlayerAct = CommandSystem.PickupItemPlayer();
                    }
                    AutoMovePlayer = false;
                }
                if (DungeonMap.GetShopAt(Player.X, Player.Y) != null)
                {

                    AutoMovePlayer = false;
                }
                if (rLMouse.GetLeftClick() || keyPress != null)
                {
                    AutoMovePlayer = false;
                }
                return didPlayerAct;
            }
            // if player requested acceleration, use the previous direction,
            // but turn off acceleration if a key is pressed to stop it.
            else if (AcceleratePlayer)
            {
                if (rLMouse.GetLeftClick() || keyPress != null)
                {
                    AcceleratePlayer = false;
                    return false;
                }
                else
                {
                    Direction direction = AccelerateDirection;
                    if (direction != Direction.None)
                    {
                        didPlayerAct = CommandSystem.MovePlayer(direction);
                    }
                    if (!didPlayerAct)
                    {
                        AcceleratePlayer = false;
                    }
                }
                return didPlayerAct;
            }
            else if (rLMouse.GetLeftClick())
            {
                if (rLMouse.X <= MapWidth && rLMouse.Y <= MapHeight)
                {
                    if (DungeonMap.GetCell(rLMouse.X, rLMouse.Y).IsExplored)
                    {
                        AutoMovePlayer = true;
                        AutoMoveXTarget = rLMouse.X;
                        AutoMoveYTarget = rLMouse.Y;
                        if (DungeonMap.GetMonsterAt(rLMouse.X, rLMouse.Y) != null)
                        {
                            AutoMoveMonsterTarget = DungeonMap.GetMonsterAt(rLMouse.X, rLMouse.Y);
                        }
                        else
                        {
                            AutoMoveMonsterTarget = null;
                        }
                    }
                    else
                    {
                        Cell target = DungeonMap.GetNearestObject("explorable", true, 
                            targetCell: DungeonMap.GetCell(rLMouse.X, rLMouse.Y));
                        if (target != null)
                        {
                            AutoMovePlayer = true;
                            AutoMoveXTarget = target.X;
                            AutoMoveYTarget = target.Y;
                        }
                    }
                }
            }
            else if (keyPress != null)
            {
                if (_quittingGame)
                {
                    _rootConsole.Close();
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
                        AccelerateDirection = direction;
                        didPlayerAct = CommandSystem.MovePlayer(direction);
                    }
                    else if (keyPress.Key == RLKey.Number1)
                    {
                        Cell target = DungeonMap.GetNearestObject("item", true);
                        if (target != null)
                        {
                            AutoMovePlayer = true;
                            AutoMoveXTarget = target.X;
                            AutoMoveYTarget = target.Y;

                        }
                    }
                    else if (keyPress.Key == RLKey.Number2)
                    {
                        Cell target = DungeonMap.GetNearestObject("door", true);
                        if (target != null)
                        {
                            AutoMovePlayer = true;
                            AutoMoveXTarget = target.X;
                            AutoMoveYTarget = target.Y;
                        }
                    }
                    else if (keyPress.Key == RLKey.Number3)
                    {
                        Cell target = DungeonMap.GetNearestObject("explorable", true);
                        if (target != null)
                        {
                            AutoMovePlayer = true;
                            AutoMoveXTarget = target.X;
                            AutoMoveYTarget = target.Y;
                        }
                    }
                    else if (keyPress.Key == RLKey.T)
                    {
                        Game.SecondaryConsoleActive = true;
                        Game.AcceleratePlayer = false;
                        Game.CurrentSecondary = new TargetingSystem("travel-info", Math.Max(MapWidth, MapHeight));
                        //Game.PostSecondary = new Instant(rune, radius:
                        //    _offensiveRadius[rune], special: "Rune");
                        //Game.MessageLog.Add("Select your target (TAB to cycle).");
                    }
                    else if (_inputSystem.CloseDoorKeyPressed(keyPress))
                    {
                        didPlayerAct = CommandSystem.CloseDoorsNextTo(Player);
                    }
                    else if (_inputSystem.QuitKeyPressed(keyPress))
                    {
                        GameOver("quit on level " + Game.mapLevel.ToString());
                        _renderRequired = true;
                    }
                    else if (_inputSystem.RuneKeyPressed(keyPress))
                    {
                        SecondaryConsoleActive = true;
                        AcceleratePlayer = false;
                        CurrentSecondary = RuneSystem;
                        _renderRequired = true;
                    }
                    else if (_inputSystem.PickupKeyPressed(keyPress))
                    {
                        didPlayerAct = CommandSystem.PickupItemPlayer();
                    }
                    else if (_inputSystem.DescendStairs(keyPress))
                    {
                        didPlayerAct = NewLevel();
                    }
                    else if (_inputSystem.WaitKey(keyPress))
                    {
                        didPlayerAct = true;
                    }
                }
                return didPlayerAct;
            }
            return false;
        }

        public static bool NewLevel()
    {
            if (!FinalLevel() && DungeonMap.CanMoveDownToNextLevel())
            {
                // MapGenerator treats Game.MaxDungeonLevel differently
                MapGenerator mapGenerator = new MapGenerator(_mapWidth, _mapHeight, 20, 13, 7, ++mapLevel);
                DungeonMap = mapGenerator.CreateMap();
                MessageLog = new MessageLog();
                MessageLog.Add("The stairs collapse behind you.");
                if (Game.FinalLevel())
                {
                    MessageLog.Add("Are you ready to ascend the throne of runes?");
                }
                CommandSystem = new CommandSystem();
                _rootConsole.Title = $"RuneRogue - Level {mapLevel}";
                return true;
            }
            else if (FinalLevel() && DungeonMap.CanMoveDownToNextLevel())
            {
                if (DungeonMap.MonstersCount() > 0)
                {
                    MessageLog.Add("You must be the sole challenger for the rune throne.");
                    return false;
                }
                else
                {
                    GameOver("WON!");
                    //MessageLog.Add($"You have won RuneRogue! Your final score is {Player.LifetimeGold * 3}.");
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        // GameOver triggers the end game sequence and takes as a parameter the reason
        // the game ended.
        public static void GameOver(string fate)
        {
            _playerFate = fate;
            _gameOver = true;
        }

        // NewScore follows GameOver. It records the score in the file HighScoreFile and
        // then displays the score and comparable scores from the file.
        public static void NewScore()
        {
            string fate = _playerFate;
            List<string> scoreList;
            string timestamp = DateTime.Now.ToString("g");
            int score = Player.LifetimeGold;
            if (fate == "WON!")
            {
                score *= 3;
            }
            if (File.Exists(HighScoreFile))
                scoreList = File.ReadAllLines(HighScoreFile).ToList();
            else
                scoreList = new List<string>();
            string record = Player.Name + " (" + fate + " on " + timestamp + "), score " + score.ToString();
            scoreList.Add(record);
            var sortedScoreList = scoreList.OrderByDescending(ss => int.Parse(ss.Substring(ss.LastIndexOf(" ") + 1)));
            List<string> sortedList = sortedScoreList.ToList();
            int rank = sortedList.IndexOf(record) + 1;
            string rankth = rank.ToString() + "th";
            if (rankth == "1th")
            {
                rankth = "1st";
            }
            else if (rankth == "2th")
            {
                rankth = "2nd";
            }
            MessageLog.Add("");
            if (sortedList.Count > 1)
            {
                MessageLog.Add($"You came in {rankth} out of {sortedList.Count} plays.");
                for (int row = Math.Max(1, rank - 1); row <= Math.Min(rank + 1, sortedList.Count); row++)
                {
                    MessageLog.Add("(" + row.ToString() + ") " + sortedList[row - 1]);
                }
                MessageLog.Draw(_messageConsole);
                RLConsole.Blit(_messageConsole, 0, 0, _messageWidth, _messageHeight, _rootConsole, 0, _screenHeight - _messageHeight);
            }
            MessageLog.Add("");
            File.WriteAllLines(HighScoreFile, sortedScoreList.ToArray());
            Game.MessageLog.Add($"Goodbye! Press any key to exit.");
            _renderRequired = true;
            AcceleratePlayer = false;
            AutoMovePlayer = false;
            _quittingGame = true;
        }

        public static void PrintDebugMessage(string message)
        {
            System.Console.WriteLine(message);
        }

        public static void PrintCellList(List<Cell> list)
        {
            string x = "";
            foreach (Cell cell in list)
            {
                x += $"({cell.X}, {cell.Y})) ";
            }
            System.Console.WriteLine(x);
        }

        
    }
}
