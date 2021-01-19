using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenTK.Graphics.ES11;
using OpenTK.Graphics.OpenGL;
using RLNET;
using RogueSharp;
using RogueSharp.DiceNotation;
using RuneRogue.Items;
using RuneRogue.Systems;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuneRogue.Core
{
    // Our custom DungeonMap class extends the base RogueSharp Map class
    public class DungeonMap : Map
    {
        private readonly List<Monster> _monsters;
        private readonly List<Item> _items;
        private int _dungeonLevel;

        // cells that are possibly adjacent to non-explored accessible cells
        private readonly List<Cell> _goodExplorationCells;
        // cells that are known not to be adjacent to those cells
        private readonly List<Cell> _badExplorationCells;

        private Map _exploredMap;

        public MapState MapState { 
            get { return Save(); }
            set { Restore(value); }
        }

        public List<Rectangle> Rooms { get; set; }
        public List<Door> Doors { get; set; }
        public List<Shop> Shops { get; set; }
        public Stairs StairsUp { get; set; }
        public Stairs StairsDown { get; set; }
        public int DungeonLevel
        {
            get { return _dungeonLevel; }
            set { _dungeonLevel = value; }
        }
        public bool FinalLevel { get { return _dungeonLevel == Systems.MapGenerator.maxDungeonLevel; } }

        public Map ExploredMap
        {
            get { return _exploredMap; }
        }

        public DungeonMap()
        {
            Game.SchedulingSystem.Clear();

            // Initialize all the lists when we create a new DungeonMap
            _monsters = new List<Monster>();
            _items = new List<Item>();
            _goodExplorationCells = new List<Cell>();
            _badExplorationCells = new List<Cell>();
            Rooms = new List<Rectangle>();
            Doors = new List<Door>();
            Shops = new List<Shop>();
            _exploredMap = new Map();
            _exploredMap.Initialize(Game.MapWidth, Game.MapHeight);
            Game.Player.PlayerPeril = false;
        }

        // This method will be called any time we move the player to update field-of-view
        public void UpdatePlayerFieldOfView()
        {
            Player player = Game.Player;
            // Compute the field-of-view based on the player's location and awareness
            ComputeFov(player.X, player.Y, player.Awareness, true);
            // Mark all cells in field-of-view as having been explored
            foreach (Cell cell in GetAllCells())
            {
                if (IsInFov(cell.X, cell.Y))
                {
                    SetCellProperties(cell.X, cell.Y, cell.IsTransparent, cell.IsWalkable, true);
                    _exploredMap.SetCellProperties(cell.X, cell.Y, cell.IsTransparent, cell.IsWalkable, true);

                    if (!_goodExplorationCells.Contains(cell) &&
                        !_badExplorationCells.Contains(cell) &&
                        (cell.IsWalkable || GetDoor(cell.X, cell.Y) != null))
                    {
                        _goodExplorationCells.Add(cell);
                    }
                }
            }
            player.PlayerPeril = (MonstersInFOV().Count == 0) ? false : player.IgnoreMonsters(MonstersInFOV());
            //player.PlayerPeril = player.IgnoreMonsters(MonstersInFOV());
        }

        public List<Monster> MonstersInFOV(bool skipInvisible = true)
        {
            List<Monster> monstersSeen = (skipInvisible) ?
                _monsters.Where(m => IsInFov(m.X, m.Y) && !m.IsInvisible).ToList() :
                _monsters.Where(m => IsInFov(m.X, m.Y)).ToList();
            // order monsters from left-most column and then by highest on screen
            monstersSeen.Sort((x, y) => (100 * x.X + x.Y).CompareTo(100 * y.X + y.Y));
            return monstersSeen;
        }

        // Returns true when able to place the Actor on the cell or false otherwise
        public bool SetActorPosition(Actor actor, int x, int y)
        {
            if (actor.IsImmobile)
            {
                return false;
            }
            // Don't open doors on acceleration.
            if (actor == Game.Player && Game.AcceleratePlayer && (GetDoor(x, y) != null))
            {
                if (!GetDoor(x, y).IsOpen)
                {
                    return false;
                }
            }
            // bump into door to open
            if (GetDoor(x, y) != null)
            {
                if (!GetDoor(x, y).IsOpen)
                {
                    OpenDoor(actor, x, y);
                    return true;
                }
            }
            // Only allow actor placement if the cell is walkable
            if (GetCell(x, y).IsWalkable)
            {
                // The cell the actor was previously on is now walkable
                SetIsWalkable(actor.X, actor.Y, true);
                // Update the actor's position
                actor.X = x;
                actor.Y = y;
                // The new cell the actor is on is now not walkable
                SetIsWalkable(actor.X, actor.Y, false);
                // Try to open a door if one exists here
                //OpenDoor(actor, x, y);
                // Don't forget to update the field of view if we just repositioned the player
                if (actor is Player)
                {
                    UpdatePlayerFieldOfView();
                }
                // stop acceleration on stairs
                if (CheckStairs(actor.X, actor.Y) && actor is Player)
                {
                    Game.AcceleratePlayer = false;
                }
                return true;
            }
            return false;
        }

        // Return the door at the x,y position or null if one is not found.
        public Door GetDoor(int x, int y)
        {
            return Doors.SingleOrDefault(d => d.X == x && d.Y == y);
        }
        public Item GetItemAt(int x, int y)
        {
            return _items.SingleOrDefault(d => d.X == x && d.Y == y);
        }
        public Shop GetShopAt(int x, int y)
        {
            return Shops.SingleOrDefault(d => d.X == x && d.Y == y);
        }
        public bool CheckStairs(int x, int y)
        {
            return (StairsUp.X == x && StairsUp.Y == y);
        }

        // The actor opens the door located at the x,y position
        private void OpenDoor(Actor actor, int x, int y)
        {
            Door door = GetDoor(x, y);
            if (door != null && !door.IsOpen)
            {
                door.IsOpen = true;
                var cell = GetCell(x, y);
                // Once the door is opened it should be marked as transparent and no longer block field-of-view
                SetCellProperties(x, y, true, cell.IsWalkable, cell.IsExplored);
                _exploredMap.SetCellProperties(x, y, cell.IsTransparent, cell.IsWalkable, true);
                UpdatePlayerFieldOfView();
            }
        }

        public void CloseDoor(Actor actor, int x, int y)
        {
            Door door = GetDoor(x, y);
            if (door != null && door.IsOpen)
            {
                door.IsOpen = false;
                var cell = GetCell(x, y);
                // Once the door is opened it should be marked as transparent and no longer block field-of-view
                SetCellProperties(x, y, false, cell.IsWalkable, cell.IsExplored);
                UpdatePlayerFieldOfView();
            }
        }

        public int MonstersCount()
        {
            return _monsters.Count;
        }

        // Called by MapGenerator after we generate a new map to add the player to the map
        public void AddPlayer(Player player)
        {
            Game.Player = player;
            SetIsWalkable(player.X, player.Y, false);
            UpdatePlayerFieldOfView();
            Game.SchedulingSystem.Add(player);
        }

        public void AddMonster(Monster monster)
        {
            _monsters.Add(monster);
            // After adding the monster to the map make sure to make the cell not walkable
            SetIsWalkable(monster.X, monster.Y, false);
            Game.SchedulingSystem.Add(monster);
        }

        public void RemoveMonster(Monster monster)
        {
            _monsters.Remove(monster);
            // After removing the monster from the map, make sure the cell is walkable again
            SetIsWalkable(monster.X, monster.Y, true);
            Game.SchedulingSystem.Remove(monster);
            ComputeFov(Game.Player.X, Game.Player.Y, Game.Player.Awareness, true);
            Game.Player.PlayerPeril = Game.Player.IgnoreMonsters(MonstersInFOV());

        }

        public void AddItem(Item item)
        {
            Item currentitem = GetItemAt(item.X, item.Y);
            // limit one item per space, generate random adjacent location if necessary
            int placementTries = 0;
            if ((currentitem != null) && !(currentitem is Gold && item is Gold))
            {
                int x = item.X;
                int y = item.Y;
                while ((GetItemAt(x, y) != null || !GetCell(x, y).IsWalkable) && placementTries < 20)
                {
                    if (placementTries < 10)
                    {
                        x = item.X + Dice.Roll("1d3-2");
                        y = item.Y + Dice.Roll("1d3-2");
                    }
                    else
                    {
                        x = item.X + Dice.Roll("1d5-3");
                        y = item.Y + Dice.Roll("1d5-3");
                    }
                    placementTries += 1;
                }
                item.X = x;
                item.Y = y;
            }
            if (placementTries < 20)
            {
                _items.Add(item);
            }
            else
            {
                Game.MessageLog.Add("could not add item");
            }
        }

        public void RemoveItem(Item item)
        {
            _items.Remove(item);
        }

        public Monster GetMonsterAt(int x, int y)
        {
            return _monsters.FirstOrDefault(m => m.X == x && m.Y == y);
        }

        public bool MissileNotBlocked(Actor shooter, Actor target)
        {
            bool shotNotBlocked = true;
            foreach (Cell cell in GetCellsAlongLine(shooter.X, shooter.Y, target.X, target.Y))
            {
                if (cell.X == shooter.X && cell.Y == shooter.Y)
                {
                    continue;
                }
                if (GetMonsterAt(cell.X, cell.Y) != null || !cell.IsTransparent)
                {
                    shotNotBlocked = false;
                }
            }
            return shotNotBlocked;
        }

        // finds object of a particular type closest to player or targetCell
        // 
        public Cell GetNearestObject(string objectType, bool previouslySeen, Cell targetCell = null)
        {
            int targetX = Game.Player.X;
            int targetY = Game.Player.Y;
            if (targetCell != null)
            {
                targetX = targetCell.X;
                targetY = targetCell.Y;
            }
            Cell nullCell = null;
            //List<Cell> cellsWithObject = new List<Cell>();
            //IOrderedEnumerable<IDrawable> cellsList = null;

            //IDrawable nearestD;
            //Item nearestI;
            //nearestI = _items.
            //    OrderBy(item => Math.Abs(item.X - targetX) + Math.Abs(item.Y - targetY)).
            //    Where(item => GetCell(item.X, item.Y).IsExplored).
            //    FirstOrDefault();
            //nearestD = (IDrawable)nearestI;
            //return GetCell(nearestD.X, nearestD.Y);
            //if (objectType == "item")
            //{
            //    cellsList = _items.
            //            OrderBy(item => Math.Abs(item.X - targetX) + Math.Abs(item.Y - targetY));
            //}
            //else if (objectType == "door")
            //{
            //    cellsList = Doors.
            //            OrderBy(item => Math.Abs(item.X - targetX) + Math.Abs(item.Y - targetY)).
            //            Where(d => GetCell(d.X, d.Y).IsExplored && !d.IsOpen).
            //            Where(d => !(d.X == targetX && d.Y == targetY)).
            //            FirstOrDefault();
            //}

            if (objectType == "item")
            {
                Item nearest;
                if (previouslySeen)
                {
                    nearest = _items.
                        OrderBy(item => Math.Abs(item.X - targetX) + Math.Abs(item.Y - targetY)).
                        Where(item => GetCell(item.X, item.Y).IsExplored).
                        FirstOrDefault();

                }
                else
                {
                    nearest = _items.
                        OrderBy(item => Math.Abs(item.X - targetX) + Math.Abs(item.Y - targetY)).FirstOrDefault();
                }
                if (nearest != null)
                {
                    return GetCell(nearest.X, nearest.Y);
                }
                else
                {
                    return nullCell;
                }
            }
            else if (objectType == "downstairs")
            {
                Cell stairsCell = GetCell(StairsDown.X, StairsDown.Y);
                if (previouslySeen && stairsCell.IsExplored)
                {
                    return stairsCell;
                }
                else if (!previouslySeen)
                {
                    return stairsCell;
                }
                return nullCell;
            }
            else if (objectType == "shop")
            {
                Shop nearest;
                if (previouslySeen)
                {
                    nearest = Shops.
                        OrderBy(item => Math.Abs(item.X - targetX) + Math.Abs(item.Y - targetY)).
                        Where(item => GetCell(item.X, item.Y).IsExplored).
                        FirstOrDefault();

                }
                else
                {
                    nearest = Shops.
                        OrderBy(item => Math.Abs(item.X - targetX) + Math.Abs(item.Y - targetY)).FirstOrDefault();
                }
                if (nearest != null)
                {
                    return GetCell(nearest.X, nearest.Y);
                }
                else
                {
                    return nullCell;
                }
            }
            else if (objectType == "monster")
            {
                Monster nearest;
                if (previouslySeen)
                {
                    nearest = _monsters.
                        OrderBy(item => Math.Abs(item.X - targetX) + Math.Abs(item.Y - targetY)).
                        Where(item => GetCell(item.X, item.Y).IsExplored).
                        FirstOrDefault();

                }
                else
                {
                    nearest = _monsters.
                        OrderBy(item => Math.Abs(item.X - targetX) + Math.Abs(item.Y - targetY)).FirstOrDefault();
                }
                if (nearest != null)
                {
                    return GetCell(nearest.X, nearest.Y);
                }
                else
                {
                    return nullCell;
                }
            }
            else if (objectType == "door")
            {
                Door nearest;
                if (previouslySeen)
                {
                    nearest = Doors.
                        OrderBy(item => Math.Abs(item.X - targetX) + Math.Abs(item.Y - targetY)).
                        Where(d => GetCell(d.X, d.Y).IsExplored && !d.IsOpen).
                        FirstOrDefault();
                    if (nearest == null)
                    {
                        return nullCell;
                    }
                    else if (nearest.X == targetX && nearest.Y == targetY)
                    {
                        nearest = Doors.
                        OrderBy(item => Math.Abs(item.X - targetX) + Math.Abs(item.Y - targetY)).
                        Where(d => GetCell(d.X, d.Y).IsExplored && !d.IsOpen).
                        ElementAtOrDefault(1);
                    }
                }
                else
                {
                    nearest = Doors.Where(d => !d.IsOpen).
                        OrderBy(item => Math.Abs(item.X - targetX) + Math.Abs(item.Y - targetY)).FirstOrDefault();
                }
                if (nearest != null)
                {
                    return GetCell(nearest.X, nearest.Y);
                }
                else
                {
                    return nullCell;
                }
            }
            else if (objectType == "explorable")
            {
                Cell nearest = null;
                List<Cell> badExplorationCells = new List<Cell>();
                foreach (Cell cell in _goodExplorationCells.
                        OrderBy(c => Math.Abs(c.X - targetX) + Math.Abs(c.Y - targetY)))
                {
                    for (int dx = -1; dx <= 1; dx += 2)
                    {
                        if (dx + cell.X < 0 || dx + cell.X > Game.MapWidth)
                        {
                            continue;
                        }
                        if (GetCell(cell.X + dx, cell.Y).IsWalkable && !GetCell(cell.X + dx, cell.Y).IsExplored)
                        {
                            nearest = GetCell(cell.X + dx, cell.Y);
                        }
                    }
                    for (int dy = -1; dy <= 1; dy += 2)
                    {
                        if (dy + cell.Y < 0 || dy + cell.Y > Game.MapHeight)
                        {
                            continue;
                        }
                        if (GetCell(cell.X, cell.Y + dy).IsWalkable && !GetCell(cell.X, cell.Y + dy).IsExplored)
                        {
                            nearest = GetCell(cell.X, cell.Y + dy);
                        }
                    }
                    if (nearest != null)
                    {
                        break;
                    }
                    else
                    {
                        badExplorationCells.Add(cell);
                    }
                }
                badExplorationCells.ForEach(c => _goodExplorationCells.Remove(c));
                badExplorationCells.ForEach(c => _badExplorationCells.Add(c));
                if (nearest == null)
                {
                    return nullCell;
                }
                else
                {
                    _exploredMap.SetCellProperties(nearest.X, nearest.Y, nearest.IsTransparent, nearest.IsWalkable, nearest.IsExplored);
                    return nearest;
                }
            }
            else
            {
                throw new ArgumentException($"Invalid objectType {objectType}.");
            }
        }

        public bool CanMoveDownToNextLevel()
        {
            Player player = Game.Player;

            return StairsDown.X == player.X && StairsDown.Y == player.Y;
        }

        // A helper method for setting the IsWalkable property on a Cell
        public void SetIsWalkable(int x, int y, bool isWalkable)
        {
            Cell cell = GetCell(x, y);
            SetCellProperties(cell.X, cell.Y, cell.IsTransparent, isWalkable, cell.IsExplored);
            _exploredMap.SetCellProperties(cell.X, cell.Y, cell.IsTransparent, isWalkable, cell.IsExplored);
        }

        // Look for a random location in the room that is walkable.
        public Point GetRandomWalkableLocationInRoom(Rectangle room)
        {
            if (DoesRoomHaveWalkableSpace(room))
            {
                for (int i = 0; i < 100; i++)
                {
                    int x = Game.Random.Next(1, room.Width - 2) + room.X;
                    int y = Game.Random.Next(1, room.Height - 2) + room.Y;
                    if (IsWalkable(x, y))
                    {
                        return new Point(x, y);
                    }
                }
            }

            // If we didn't find a walkable location in the room return null
            return null;
        }

        // Iterate through each Cell in the room and return true if any are walkable
        public bool DoesRoomHaveWalkableSpace(Rectangle room)
        {
            for (int x = 1; x <= room.Width - 2; x++)
            {
                for (int y = 1; y <= room.Height - 2; y++)
                {
                    if (IsWalkable(x + room.X, y + room.Y))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // The Draw method will be called each time the map is updated
        // It will render all of the symbols/colors for each cell to the map sub console
        public void Draw(RLConsole mapConsole, RLConsole statConsole, List<Cell> highlightContentsCells = null)
        {
            // replace null list with empty one
            highlightContentsCells = highlightContentsCells ?? new List<Cell>();

            Player player = Game.Player;

            foreach (Cell cell in GetAllCells())
            {
                SetConsoleSymbolForCell(mapConsole, cell);
            }

            foreach (Door door in Doors)
            {
                door.Draw(mapConsole, this);
            }

            foreach (Shop shop in Shops)
            {
                shop.Draw(mapConsole, this);
            }

            foreach (Item item in _items)
            {
                item.Draw(mapConsole, this);
            }

            StairsUp.Draw(mapConsole, this);
            StairsDown.Draw(mapConsole, this);

            // Keep an index so we know which position to draw monster stats at
            int i = 0;
            // Iterate through each monster on the map and draw it after drawing the Cells
            List<Monster> monstersSeen = (player.SASenseThoughts) ? 
                _monsters.Where(m => m.WithinDistance(player, RunesSystem.DistanceSenseThoughts)).ToList() : 
                MonstersInFOV();
            monstersSeen.Sort((x, y) => (100 * x.X + x.Y).CompareTo(100 * y.X + y.Y));
            //foreach (Monster monster in _monsters)
            foreach (Monster monster in monstersSeen)
            {
                //if (!monster.IsInvisible || player.SASenseThoughts)
                //{
                    monster.Draw(mapConsole, this);

                    // When the monster is in the field-of-view also draw their stats
                    if (IsInFov(monster.X, monster.Y))
                    {
                        // Pass in the index to DrawStats and increment it afterwards
                        bool highlightMonster = highlightContentsCells.Contains(GetCell(monster.X, monster.Y));
                        monster.DrawStats(statConsole, i,
                            highlight: highlightMonster);
                        i++;
                    }
                //}
            }
        }

        private void SetConsoleSymbolForCell(RLConsole console, Cell cell)
        {
            // When we haven't explored a cell yet, we don't want to draw anything
            if (!cell.IsExplored)
            {
                return;
            }

            // When a cell is currently in the field-of-view it should be drawn with lighter colors
            if (IsInFov(cell.X, cell.Y))
            {
                // Choose the symbol to draw based on if the cell is walkable or not
                // '.' for floor and '#' for walls. Cells with monsters get written over
                // later in the draw process
                if (cell.IsWalkable || GetMonsterAt(cell.X, cell.Y) != null)
                {
                    console.Set(cell.X, cell.Y, Colors.FloorFov, Colors.FloorBackgroundFov, '.');
                }
                else
                {
                    console.Set(cell.X, cell.Y, Colors.WallFov, Colors.WallBackgroundFov, '#');
                }
            }
            // When a cell is outside of the field of view draw it with darker colors
            else
            {
                if (cell.IsWalkable || GetMonsterAt(cell.X, cell.Y) != null)
                {
                    console.Set(cell.X, cell.Y, Colors.Floor, Colors.FloorBackground, '.');
                }
                else
                {
                    console.Set(cell.X, cell.Y, Colors.Wall, Colors.WallBackground, '#');
                }
            }
        }

        public void SaveLevel()
        {
            JsonSerializerOptions jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip,
                IgnoreNullValues = true
            };
            //Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            string jsonString = JsonSerializer.Serialize(this, options: jsonOptions);
            Game.PrintDebugMessage(jsonString);
            //XmlSerializer serializer = new XmlSerializer(typeof(DungeonMap));
            //var sb = new StringBuilder();
            //using (var sr = new System.IO.StringWriter(sb))
            //{
            // Seriaize the data.
            //    serializer.Serialize(sr, this);
            //}
            File.WriteAllText("test-sav.json", jsonString);
        }
    }
}
