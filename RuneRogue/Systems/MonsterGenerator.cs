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
    class MonsterNotFound : Exception
    {
        public MonsterNotFound(string message)
            : base(message)
        {
        }
    }

    class MonsterConstructionFailure : Exception
    {
        public MonsterConstructionFailure(string message)
            : base(message)
        {
        }
    }

    public class MonsterGenerator
    {
        private List<string> _monsterKinds;
        private Dictionary<string, MonsterStats> _fiendFolio;

        private string _monsterDataFile;
        private JsonSerializerOptions _jsonOptions;

        public string MonsterDataFile
        {
            get { return _monsterDataFile; }
            set { _monsterDataFile = value; }
        }
 
        public List<string> MonsterKinds
        {
            get { return _monsterKinds; }
        }

        public Dictionary<string, MonsterStats> FiendFolio
        {
            get { return _fiendFolio; }
        }

        public MonsterGenerator()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                ReadCommentHandling = JsonCommentHandling.Skip,
                IgnoreNullValues = true
            };
            // serialize enums as strings
            _jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        }

        public void ReadMonsterData(string FileName)
        {
            MonsterDataFile = FileName;
            string jsonString = File.ReadAllText(MonsterDataFile);
            MonsterStats[] monsterManual = JsonSerializer.Deserialize<MonsterStats[]>(jsonString, _jsonOptions);
            _fiendFolio = new Dictionary<string, MonsterStats>();
            string lastKind = "";
            foreach (MonsterStats m in monsterManual)
            {
                if (m.Kind == null && lastKind != "")
                {
                    throw new MonsterDataFormatInvalid($"Monster missing kind definition (after {lastKind}).");
                }
                else if (m.Kind == null)
                {
                    throw new MonsterDataFormatInvalid($"Initial monster is missing kind definition.");
                }
                if (_fiendFolio.ContainsKey(m.Kind))
                {
                    throw new MonsterDataFormatInvalid($"Duplicate monster entries for {m.Kind}.");
                }
                else
                {
                    _fiendFolio[m.Kind] = m;
                    lastKind = m.Kind;
                }
            }
            _monsterKinds = new List<string>(_fiendFolio.Keys);
            foreach (string m in _monsterKinds)
            {
                _fiendFolio[m].CheckDefinition();
            }
        }

        public void WriteMonsterData(string FileName)
        {
            MonsterStats[] monsterManual = _fiendFolio.Values.ToArray();
            string jsonString = JsonSerializer.Serialize(monsterManual, _jsonOptions);
            File.WriteAllText(FileName, jsonString);
        }

        public void PrintMonsterJSONString(string monster)
        {
            MonsterStats data = _fiendFolio[monster];
            string jsonString = JsonSerializer.Serialize(data, _jsonOptions);
            Console.WriteLine(jsonString);
        }

        public void PrintMonsterJSONString(MonsterStats monster)
        {
            string jsonString = JsonSerializer.Serialize(monster, _jsonOptions);
            Console.WriteLine(jsonString);
        }

        // useful function for examining how different data structures get serialized
        public void PrintDataStructureSerialization(object data)
        {
            string jsonString = JsonSerializer.Serialize(data, _jsonOptions);
            Console.WriteLine(jsonString);
        }


        public List<Monster> CreateEncounter(int DungeonLevel)
        {
            string monsterToAdd = null;
            MonsterStats monsterType = null;

            bool rerollMonster = true;
            while (rerollMonster)
            {
                monsterToAdd = (string)Game.RandomArrayValue(MonsterKinds.ToArray());
                rerollMonster = false;

                if (!_fiendFolio.TryGetValue(monsterToAdd, out monsterType))
                {
                    throw new MonsterNotFound($"{monsterToAdd} monster statistics not in Fiend Folio.");
                }

                if (DungeonLevel < monsterType.MinLevel || DungeonLevel > monsterType.MaxLevel)
                {
                    rerollMonster = true;
                }
                if (monsterType.IsUnique && monsterType.NumberGenerated > 0)
                {
                    rerollMonster = true;
                }
                // Rarity is percent chance that monster will not show compared to monsters without rarity defined
                // on a level (who have default rarity 0). Check it.
                int rarityRoll = Dice.Roll("1d200") - 100;
                if (rarityRoll < monsterType.Rarity)
                {
                    rerollMonster = true;
                }
            }

            return CreateEncounter(DungeonLevel, monsterToAdd, monsterType.NumberAppearing);
        }

        public List<Monster> CreateEncounter(int DungeonLevel, string monsterToAdd, string monsterNumAppearingDice)
        {
            List<Monster> encounterMonsters = new List<Monster>();
            if (!_fiendFolio.TryGetValue(monsterToAdd, out MonsterStats monsterType))
            {
                throw new MonsterNotFound($"{monsterToAdd} monster statistics not in Fiend Folio.");
            }
            monsterType.NumberGenerated++;

            int numberOfMonsters = Dice.Roll(monsterNumAppearingDice);

            //monsterType = _fiendFolio["beetleclone"];

            for (int i = 0; i < numberOfMonsters; i++)
            {
                Monster monster = monsterType.CreateMonster();
                encounterMonsters.Add(monster);
                if (!(monsterType.FollowerKinds == null))
                {
                    for (int j=0; j < monsterType.FollowerKinds.Length; j++)
                    {
                        // check if should generate this follower type
                        if (Dice.Roll("1D100") <= monsterType.FollowerProbability[j])
                        {
                            List<Monster> followerMonsters = CreateEncounter(DungeonLevel,
                                monsterType.FollowerKinds[j],
                                monsterType.FollowerNumberAppearing[j]);
                            encounterMonsters.AddRange(followerMonsters);
                        }
                    }
                }
            }
            return encounterMonsters;
        }

    }
}
