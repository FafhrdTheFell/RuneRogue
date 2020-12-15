using System;
using System.Collections.Generic;
using System.Linq;
using RogueSharp;
using RogueSharp.DiceNotation;
using RuneRogue.Core;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Text.Encodings.Web;

namespace RuneRogue.Systems
{
    class MonsterDataFormatInvalid : Exception
    {
        public MonsterDataFormatInvalid(string message)
            : base(message)
        {
        }
    }


    public class MonsterGenerator
    {
        private MonsterStats[] _monsterManual;
        private Dictionary<string, int> _manualPage;
        private Array _monsterKinds;

        private string _monsterDataFile;
        private JsonSerializerOptions _jsonOptions;

        public string MonsterDataFile
        {
            get { return _monsterDataFile; }
            set { _monsterDataFile = value; }
        }
 
        public Array MonsterKinds
        {
            get { return _monsterKinds; }
        }

        public MonsterGenerator()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                ReadCommentHandling = JsonCommentHandling.Skip,
            };
            // serialize enums as strings
            _jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        }

        public void ReadMonsterData(string FileName)
        {
            MonsterDataFile = FileName;
            string jsonString = File.ReadAllText(MonsterDataFile);
            _monsterManual = JsonSerializer.Deserialize<MonsterStats[]>(jsonString, _jsonOptions);
            GenerateKinds();
            GenerateIndex();
        }

        public void WriteMonsterData(string FileName)
        {
            string jsonString = JsonSerializer.Serialize(_monsterManual, _jsonOptions);
            File.WriteAllText(FileName, jsonString);
        }

        public void PrintMonsterString(string monster)
        {
            MonsterStats data = _monsterManual[_manualPage[monster]];
            string jsonString = JsonSerializer.Serialize(data, _jsonOptions);
            Console.WriteLine(jsonString);
        }

        // useful function for examining how different data structures get serialized
        public void PrintDataStructure(object data)
        {
            string jsonString = JsonSerializer.Serialize(data, _jsonOptions);
            Console.WriteLine(jsonString);
        }

        public void GenerateIndex()
        {
            _manualPage = new Dictionary<string, int>();
            for (int i = 0; i < _monsterManual.Length; i++)
            {
                _manualPage.Add(_monsterManual[i].Kind, i);
            }
        }

        public void GenerateKinds()
        {
            List<string> monsterKinds = new List<string>();
            for (int i = 0; i < _monsterManual.Length; i++)
            {
                monsterKinds.Add(_monsterManual[i].Kind);
                // check that followers data formatted correctly
                // could do more checks here
                if (!(_monsterManual[i].FollowerKinds == null))
                {
                    bool followerFormatBad =
                        (!(_monsterManual[i].FollowerKinds.Length == _monsterManual[i].FollowerNumberAppearing.Length) ||
                        !(_monsterManual[i].FollowerKinds.Length == _monsterManual[i].FollowerProbability.Length));
                    if (followerFormatBad)
                    {
                        throw new MonsterDataFormatInvalid($"{_monsterManual[i].Name} follower arrays of different lengths.");
                    }
                    // if own type is follower and has 100% chance of being generated, causes infinite loop.
                    // note that an infinite loop also occurs if monster1 is follower of monster2 is follower
                    // of monster1, but program does not check for that.
                    for (int j = 0; j < _monsterManual[i].FollowerKinds.Length; j++)
                    {
                        if ((_monsterManual[i].FollowerKinds[j] == _monsterManual[i].Kind))
                        {
                            throw new MonsterDataFormatInvalid($"{_monsterManual[i].Name} creates itself as follower," +
                                " risking infinite loop.");
                        }
                    }
                }
            }
            bool isUnique = monsterKinds.Distinct().Count() == monsterKinds.Count();
            if (isUnique)
            {
                _monsterKinds = monsterKinds.ToArray();
            }
            else
            {
                throw new MonsterDataFormatInvalid($"Monster kinds values not unique: {monsterKinds.ToArray()}.");
            }
        }

        // if monster = GEN, generate a level-appropriate monster list, else if
        // monster is specified, generate a list of those monsters and their followers,
        // if any
        public List<Monster> CreateEncounter(int DungeonLevel, string monsterToAdd="GEN", string monsterNumAppearing = "GEN")
        {
            List<Monster> encounterMonsters = new List<Monster>();
            string monsterType = "compiler complains if not set";
            string numInRoomDice; // = "compiler complains if not set";
            Monster monster = new Monster();

            if (monsterToAdd == "GEN")
            {
                bool rerollMonster = true;
                while (rerollMonster)
                {
                    monsterType = (string)Game.RandomArrayValue(MonsterKinds);
                    rerollMonster = false;

                    monster = CreateMonster(monsterType);
                    
                    if (DungeonLevel < monster.MinLevel || DungeonLevel > monster.MaxLevel)
                    {
                        rerollMonster = true;
                    }
                    // EncounterRarity is percent chance to find relative to most common
                    // monsters on a level. Check it.
                    if (!(monster.EncounterRarity == 0))
                    {
                        int rarityRoll = Dice.Roll("1d100");
                        if (monster.EncounterRarity <= rarityRoll)
                        {
                            rerollMonster = true;
                        }
                    }
                }
            }
            else
            {
                monsterType = monsterToAdd;
                monster = CreateMonster(monsterType);
            }
            if (monsterNumAppearing == "GEN")
            {
                numInRoomDice = monster.NumberAppearing;
            }
            else
            {
                numInRoomDice = monsterNumAppearing;
            }

            int numberOfMonsters = Dice.Roll(numInRoomDice);

            //monsterType = "beetle";

            for (int i = 0; i < numberOfMonsters; i++)
            {

                monster = CreateMonster(monsterType);
                encounterMonsters.Add(monster);
                if (!(monster.FollowerKinds == null))
                {
                    for (int j=0; j < monster.FollowerKinds.Length; j++)
                    {
                        // check if should generate this follower type
                        if (Dice.Roll("1D100") <= monster.FollowerProbability[j])
                        {
                            List<Monster> followerMonsters = CreateEncounter(DungeonLevel,
                                monster.FollowerKinds[j],
                                monster.FollowerNumberAppearing[j]);
                            encounterMonsters.AddRange(followerMonsters);
                        }
                    }
                }
            }

            return encounterMonsters;
        }

        public Monster CreateMonster(string monsterKind)
        {
            Monster monster = new Monster();
            int page = _manualPage[monsterKind];
            monster.Attack = Dice.Roll(_monsterManual[page].Attack);
            monster.AttackChance = Dice.Roll(_monsterManual[page].AttackChance);
            monster.AttackSkill = monster.AttackChance / 10;
            monster.MissileAttack = _monsterManual[page].MissileAttack;
            monster.MissileRange = _monsterManual[page].MissileRange;
            monster.MissileType = _monsterManual[page].MissileType;
            monster.SpecialAttackRange = _monsterManual[page].SpecialAttackRange;
            monster.SpecialAttackType = _monsterManual[page].SpecialAttackType;
            monster.Awareness = _monsterManual[page].Awareness;
            monster.Color = Colors.ColorLookup(_monsterManual[page].Color);
            monster.Defense = Dice.Roll(_monsterManual[page].Defense);
            monster.DefenseChance = Dice.Roll(_monsterManual[page].DefenseChance);
            monster.DefenseSkill = monster.DefenseChance / 10;
            monster.Gold = Dice.Roll(_monsterManual[page].Gold);
            monster.MaxHealth = Dice.Roll(_monsterManual[page].MaxHealth);
            monster.Health = monster.MaxHealth;
            monster.Name = _monsterManual[page].Name;
            monster.Speed = _monsterManual[page].Speed;
            monster.Symbol = _monsterManual[page].Symbol;
            monster.NumberAppearing = _monsterManual[page].NumberAppearing;
            monster.MinLevel = _monsterManual[page].MinLevel;
            monster.MaxLevel = _monsterManual[page].MaxLevel;
            monster.SAFerocious = _monsterManual[page].SAFerocious;
            monster.SALifedrainOnHit = _monsterManual[page].SALifedrainOnHit;
            monster.SALifedrainOnDamage = _monsterManual[page].SALifedrainOnDamage; 
            monster.SARegeneration = _monsterManual[page].SARegeneration;
            monster.SAVampiric = _monsterManual[page].SAVampiric;
            monster.SAVenomous = _monsterManual[page].SAVenomous;
            monster.SACausesStun = _monsterManual[page].SACausesStun;
            monster.SADoppelganger = _monsterManual[page].SADoppelganger;
            monster.SAHighImpact = _monsterManual[page].SAHighImpact;
            monster.IsUndead = _monsterManual[page].IsUndead;
            monster.IsImmobile = _monsterManual[page].IsImmobile;
            monster.FollowerKinds = _monsterManual[page].FollowerKinds;
            monster.FollowerNumberAppearing = _monsterManual[page].FollowerNumberAppearing;
            monster.FollowerProbability = _monsterManual[page].FollowerProbability;
            monster.EncounterRarity = _monsterManual[page].EncounterRarity;

            return monster;
        }
    }
}
