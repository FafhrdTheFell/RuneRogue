﻿using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.Remoting.Messaging;
using RogueSharp;
using RogueSharp.DiceNotation;
using RuneRogue.Core;
using RuneRogue.Items;
using RuneRogue.Shops;

namespace RuneRogue.Systems
{
    public class MapGenerator
    {
        private readonly int _width;
        private readonly int _height;
        private readonly int _maxRooms;
        private readonly int _roomMaxSize;
        private readonly int _roomMinSize;
        private readonly int _mapLevel;

        private readonly DungeonMap _map;

        public static int maxDungeonLevel = 14;

        // random type of shop on _shopLevels
        private static List<int> _shopLevels = new List<int>
        {
            1,3,6,9,12
        };
        private static List<int> _runeForgeLevels = new List<int>
        {
            1,4,8,12,14
        };
        private static List<int> _bookShopLevels = new List<int>
        {
            5,9
        };
        // dictionary from levels to (1) monster kind, (2) # appearing, (3) exclusive or additional
        // exclusive = only monsters to spawn additional = spawn more monsters. Length 3 string
        // arrays spawn 1 encounter, length 6 spawn two etc.
        private static Dictionary<int, string[]> _levelMonsterAdds = new Dictionary<int, string[]>
        {
            [maxDungeonLevel] = new string[] { "corruptedtitan", "1d3+1", "exclusive" }
        };

        // Constructing a new MapGenerator requires the dimensions of the maps it will create
        // as well as the sizes and maximum number of rooms
        public MapGenerator(int width, int height, int maxRooms, int roomMaxSize, int roomMinSize, int mapLevel)
        {
            _width = width;
            _height = height;
            _maxRooms = maxRooms;
            _mapLevel = mapLevel;
            if (mapLevel != maxDungeonLevel)
            {
                _maxRooms = maxRooms;
                _roomMaxSize = roomMaxSize;
                _roomMinSize = roomMinSize;
            }
            else
            {
                // lowest dungeon level is a single large room
                _maxRooms = 1;
                _roomMaxSize = 40;
                _roomMinSize = 30;
            }
            _map = new DungeonMap();
        }

        // Generate a new map that places rooms randomly
        public DungeonMap CreateMap()
        {
            // Set the properties of all cells to false
            _map.Initialize(_width, _height);

            _map.DungeonLevel = _mapLevel;

            // Try to place as many rooms as the specified maxRooms
            for (int r = 0; r < _maxRooms; r++)
            {
                // Determine a the size and position of the room randomly
                int roomWidth = Game.Random.Next(_roomMinSize, _roomMaxSize);
                int roomHeight = Game.Random.Next(_roomMinSize, _roomMaxSize);
                // bug with rooms at minumum or maximum X or Y: IsWallSpace and
                // IsPotentialDoor check non-existent cells
                int roomXPosition = Game.Random.Next(1, _width - roomWidth - 2);
                int roomYPosition = Game.Random.Next(1, _height - roomHeight - 2);

                // All of our rooms can be represented as Rectangles
                var newRoom = new Rectangle(roomXPosition, roomYPosition, roomWidth, roomHeight);

                // Check to see if the room rectangle intersects with any other rooms
                bool newRoomIntersects = _map.Rooms.Any(room => newRoom.Intersects(room));
                // As long as it doesn't intersect add it to the list of rooms. Otherwise throw it out
                if (!newRoomIntersects)
                {
                    _map.Rooms.Add(newRoom);
                }
            }

            // Iterate through each room that was generated
            for (int r = 0; r < _map.Rooms.Count; r++)
            {
                // Don't do anything with the first room
                if (r == 0)
                {
                    continue;
                }

                // For all remaing rooms get the center of the room and the previous room
                int previousRoomCenterX = _map.Rooms[r - 1].Center.X;
                int previousRoomCenterY = _map.Rooms[r - 1].Center.Y;
                int currentRoomCenterX = _map.Rooms[r].Center.X;
                int currentRoomCenterY = _map.Rooms[r].Center.Y;

                // Give a 50/50 chance of which 'L' shaped connecting cooridors to make
                if (Game.Random.Next(1, 2) == 1)
                {
                    CreateHorizontalTunnel(previousRoomCenterX, currentRoomCenterX, previousRoomCenterY);
                    CreateVerticalTunnel(previousRoomCenterY, currentRoomCenterY, currentRoomCenterX);
                }
                else
                {
                    CreateVerticalTunnel(previousRoomCenterY, currentRoomCenterY, previousRoomCenterX);
                    CreateHorizontalTunnel(previousRoomCenterX, currentRoomCenterX, currentRoomCenterY);
                }
            }
            
            // Iterate through each room that we wanted placed
            // and dig out the room and create doors for it.
            foreach (Rectangle room in _map.Rooms)
            {
                CreateRoom(room);
                CreateShop(room);
                CreateItems(room);
                CreateDoors(room);
            }

            // generate a shop in a random room every Game.ShopEveryNLevels, starting at level 1
            if (_shopLevels.Contains(_mapLevel))
            {
                CreateShop(RandomRoom(), 100);
            }

            if (_runeForgeLevels.Contains(_mapLevel))
            {
                CreateShop(RandomRoom(), 100, "RuneForge");
            }

            if (_bookShopLevels.Contains(_mapLevel))
            {
                CreateShop(RandomRoom(), 100, "BookShop");
            }


            CreateStairs();

            PlacePlayer();

            bool spawnMoreMonsters = true;
            if (_levelMonsterAdds.ContainsKey(_mapLevel))
            {
                string[] encounter = _levelMonsterAdds[_mapLevel]; 
                List<Monster> monsters = Game.MonsterGenerator.CreateEncounter(_mapLevel, encounter[0], encounter[1]);
                if (encounter[2] == "exclusive")
                {
                    spawnMoreMonsters = false;
                }
                AddMonstersToRoom(RandomRoom(), monsters);
            }
            // Final level monsters already placed
            if (spawnMoreMonsters)
            {
                PlaceMonsters();
            }

            // for debugging:
            //List<Monster> monsters2 = Game.MonsterGenerator.CreateEncounter(_mapLevel);
            //AddMonstersToRoom(_map.Rooms[0], monsters2);
       

            return _map;
        }

        // Given a rectangular area on the map
        // set the cell properties for that area to true
        private void CreateRoom(Rectangle room)
        {
            for (int x = room.Left + 1; x < room.Right; x++)
            {
                for (int y = room.Top + 1; y < room.Bottom; y++)
                {
                    _map.SetCellProperties(x, y, true, true);
                }
            }
        }

        // Carve a tunnel out of the map parallel to the x-axis
        private void CreateHorizontalTunnel(int xStart, int xEnd, int yPosition)
        {
            for (int x = Math.Min(xStart, xEnd); x <= Math.Max(xStart, xEnd); x++)
            {
                _map.SetCellProperties(x, yPosition, true, true);
            }
        }

        // Carve a tunnel out of the map parallel to the y-axis
        private void CreateVerticalTunnel(int yStart, int yEnd, int xPosition)
        {
            for (int y = Math.Min(yStart, yEnd); y <= Math.Max(yStart, yEnd); y++)
            {
                _map.SetCellProperties(xPosition, y, true, true);
            }
        }

        private void CreateShop(Rectangle room, int shopChance=5, string shopType="random")
        {
            // The the boundaries of the room
            int xMin = room.Left;
            int xMax = room.Right;
            int yMin = room.Top;
            int yMax = room.Bottom;

            // Put the rooms border cells into a list
            List<Cell> borderCells = _map.GetCellsAlongLine(xMin, yMin, xMax, yMin).ToList();
            borderCells.AddRange(_map.GetCellsAlongLine(xMin, yMin, xMin, yMax));
            borderCells.AddRange(_map.GetCellsAlongLine(xMin, yMax, xMax, yMax));
            borderCells.AddRange(_map.GetCellsAlongLine(xMax, yMin, xMax, yMax));

            List<Cell> validPositions = new List<Cell>();

            // Go through each of the rooms border cells and look for locations to place shops.
            foreach (Cell cell in borderCells)
            {
                if (IsWallSpace(cell))
                {
                    validPositions.Add(cell);
                }
            }

            Array v = validPositions.ToArray();
            Cell shopCell = (Cell)v.GetValue(Game.Random.Next(v.Length - 1));

            // Each room has a 5% chance of having a shop
            if (Dice.Roll("1D100") <= shopChance)
            {
                // 20/30/50 RuneForge, EquipmentShop, or BookShop
                int rollShopType = Dice.Roll("1D100");
                switch (shopType)
                {
                    case "RuneForge":
                        rollShopType = 10;
                        break;
                    case "EquipmentShop":
                        rollShopType = 40;
                        break;
                    case "BookShop":
                        rollShopType = 70;
                        break;
                    case "random":
                        break;
                    default:
                        throw new ArgumentException($"shopType {shopType} not valid.");
                }

                if (rollShopType <= 20)
                {
                    _map.Shops.Add(new RuneForge
                    {
                        X = shopCell.X,
                        Y = shopCell.Y
                    });
                    _map.SetCellProperties(shopCell.X, shopCell.Y, false, false);
                }
                else if (rollShopType <= 50)
                {
                    _map.Shops.Add(new EquipmentShop
                    {
                        X = shopCell.X,
                        Y = shopCell.Y
                    });
                    _map.SetCellProperties(shopCell.X, shopCell.Y, false, false);
                }
                else
                {
                    _map.Shops.Add(new BookShop
                    {
                        X = shopCell.X,
                        Y = shopCell.Y
                    });
                    _map.SetCellProperties(shopCell.X, shopCell.Y, false, false);
                }
            }
        }

        private void CreateDoors(Rectangle room)
        {
            // The the boundaries of the room
            int xMin = room.Left;
            int xMax = room.Right;
            int yMin = room.Top;
            int yMax = room.Bottom;

            // Put the rooms border cells into a list
            List<Cell> borderCells = _map.GetCellsAlongLine(xMin, yMin, xMax, yMin).ToList();
            borderCells.AddRange(_map.GetCellsAlongLine(xMin, yMin, xMin, yMax));
            borderCells.AddRange(_map.GetCellsAlongLine(xMin, yMax, xMax, yMax));
            borderCells.AddRange(_map.GetCellsAlongLine(xMax, yMin, xMax, yMax));

            // Go through each of the rooms border cells and look for locations to place doors.
            foreach (Cell cell in borderCells)
            {
                if (IsPotentialDoor(cell))
                {
                    // A door must block field-of-view when it is closed.
                    _map.SetCellProperties(cell.X, cell.Y, false, true);
                    _map.Doors.Add(new Door
                    {
                        X = cell.X,
                        Y = cell.Y,
                        IsOpen = false
                    });
                }
            }
        }

        // Checks to see if a cell is a good candidate for placement of a door
        private bool IsPotentialDoor(Cell cell)
        {
            // If the cell is not walkable
            // then it is a wall and not a good place for a door
            if (!cell.IsWalkable)
            {
                return false;
            }

            // Store references to all of the neighboring cells 
            Cell right = _map.GetCell(cell.X + 1, cell.Y);
            Cell left = _map.GetCell(cell.X - 1, cell.Y);
            Cell top = _map.GetCell(cell.X, cell.Y - 1);
            Cell bottom = _map.GetCell(cell.X, cell.Y + 1);

            // Make sure there is not already a door here
            if (_map.GetDoor(cell.X, cell.Y) != null ||
                 _map.GetDoor(right.X, right.Y) != null ||
                 _map.GetDoor(left.X, left.Y) != null ||
                 _map.GetDoor(top.X, top.Y) != null ||
                 _map.GetDoor(bottom.X, bottom.Y) != null)
            {
                return false;
            }

            // This is a good place for a door on the left or right side of the room
            if (right.IsWalkable && left.IsWalkable && !top.IsWalkable && !bottom.IsWalkable)
            {
                return true;
            }

            // This is a good place for a door on the top or bottom of the room
            if (!right.IsWalkable && !left.IsWalkable && top.IsWalkable && bottom.IsWalkable)
            {
                return true;
            }
            return false;
        }

        private bool IsWallSpace (Cell cell)
        {
            if (cell.IsWalkable)
            {
                return false;
            }

            // Store references to all of the neighboring cells 
            Cell right = _map.GetCell(cell.X + 1, cell.Y);
            Cell left = _map.GetCell(cell.X - 1, cell.Y);
            Cell top = _map.GetCell(cell.X, cell.Y - 1);
            Cell bottom = _map.GetCell(cell.X, cell.Y + 1);

            // Make sure there is not already a door here
            if (_map.GetDoor(cell.X, cell.Y) != null ||
                 _map.GetDoor(right.X, right.Y) != null ||
                 _map.GetDoor(left.X, left.Y) != null ||
                 _map.GetDoor(top.X, top.Y) != null ||
                 _map.GetDoor(bottom.X, bottom.Y) != null)
            {
                return false;
            }

            // This is a good place for a hole in the wall if only one direction walkable
            if ((right.IsWalkable && !left.IsWalkable && !top.IsWalkable && !bottom.IsWalkable) ||
                (!right.IsWalkable && left.IsWalkable && !top.IsWalkable && !bottom.IsWalkable) ||
                (!right.IsWalkable && !left.IsWalkable && top.IsWalkable && !bottom.IsWalkable) ||
                (!right.IsWalkable && !left.IsWalkable && !top.IsWalkable && bottom.IsWalkable))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public Rectangle RandomRoom()
        {
            int roomIndex = Dice.Roll("1d" + _map.Rooms.Count) - 1;
            return _map.Rooms[roomIndex];
        }

        public void CreateItems(Rectangle room)
        {
            // 4% chance of gold
            if (Dice.Roll("1d100") <= 4)
            {
                Gold gold = new Gold(Dice.Roll("1d6+2d" + (_mapLevel * 2).ToString()));

                Point randomRoomLocation = _map.GetRandomWalkableLocationInRoom(room);
                if (randomRoomLocation != null)
                {
                    gold.X = randomRoomLocation.X;
                    gold.Y = randomRoomLocation.Y;
                    _map.AddItem(gold);
                }
            }
            // 10% chance of valuables
            if (Dice.Roll("1d100") <= 10)
            {
                Valuable bling = new Valuable();

                Point randomRoomLocation = _map.GetRandomWalkableLocationInRoom(room);
                if (randomRoomLocation != null)
                {
                    bling.X = randomRoomLocation.X;
                    bling.Y = randomRoomLocation.Y;
                    _map.AddItem(bling);
                }
            }
        }

    // Find the center of the first room that we created and place the Player there
    private void PlacePlayer()
        {
            Player player = Game.Player;
            if (player == null)
            {
                player = new Player();
            }

            player.X = _map.Rooms[0].Center.X;
            player.Y = _map.Rooms[0].Center.Y;
            if (Game.FinalLevel())
            {
                // do not place player on the throne
                player.X += 2;
            }

            _map.AddPlayer(player);
        }


        private void CreateStairs()
        {
            _map.StairsUp = new Stairs
            {
                X = _map.Rooms.First().Center.X + 1,
                Y = _map.Rooms.First().Center.Y,
                IsUp = true
            };
            _map.StairsDown = new Stairs
            {
                X = _map.Rooms.Last().Center.X,
                Y = _map.Rooms.Last().Center.Y,
                IsUp = false
            };
        }

        private void PlaceMonsters()
        {
            foreach (var room in _map.Rooms)
            {
                //no monsters in first room
                if (room == _map.Rooms.First() && _mapLevel == 1)
                {
                    continue;
                }

                // Each room has a 60% chance of having monsters
                if (Dice.Roll("1D10") < 7)
                {
                    // to spawn particular monsters for testing, uncomment the next line
                    //List<Monster> monsters = Game.MonsterGenerator.CreateEncounter(_mapLevel, "shockerant");
                    List<Monster> monsters = Game.MonsterGenerator.CreateEncounter(_mapLevel);

                    AddMonstersToRoom(room, monsters);
                }
            }
        }

        private void AddMonstersToRoom(Rectangle room, List<Monster> monsters)
        {
            foreach (Monster monster in monsters)
            {
                // Find a random walkable location in the room to place the monster
                Point randomRoomLocation = _map.GetRandomWalkableLocationInRoom(room);
                // It's possible that the room doesn't have space to place a monster
                // In that case skip creating the monster
                if (randomRoomLocation != null)
                {
                    monster.X = randomRoomLocation.X;
                    monster.Y = randomRoomLocation.Y;
                    _map.AddMonster(monster);
                }
            }
        }
    }
}
