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
            MonsterStats[] monsterManual = JsonSerializer.Deserialize<MonsterStats[]>(jsonString, _jsonOptions);
            _fiendFolio = new Dictionary<string, MonsterStats>();
            foreach (MonsterStats m in monsterManual)
            {
                if (_fiendFolio.ContainsKey(m.Name))
                {
                    throw new MonsterDataFormatInvalid($"Duplicate monster entries for {m.Name}.");
                }
                else
                {
                    _fiendFolio[m.Kind] = m;
                    m.CheckDefinition();
                }
            }
            _monsterKinds = new List<string>(_fiendFolio.Keys);
        }

        public void WriteMonsterData(string FileName)
        {
            MonsterStats[] monsterManual = _fiendFolio.Values.ToArray();
            string jsonString = JsonSerializer.Serialize(monsterManual, _jsonOptions);
            File.WriteAllText(FileName, jsonString);
        }

        public void PrintMonsterString(string monster)
        {
            MonsterStats data = _fiendFolio[monster];
            string jsonString = JsonSerializer.Serialize(data, _jsonOptions);
            Console.WriteLine(jsonString);
        }

        // useful function for examining how different data structures get serialized
        public void PrintDataStructure(object data)
        {
            string jsonString = JsonSerializer.Serialize(data, _jsonOptions);
            Console.WriteLine(jsonString);
        }


        // if monster = GEN, generate a level-appropriate monster list, else if
        // monster is specified, generate a list of those monsters and their followers,
        // if any
        public List<Monster> CreateEncounter(int DungeonLevel, string monsterToAdd = "GEN", string monsterNumAppearing = "GEN")
        {
            List<Monster> encounterMonsters = new List<Monster>();
            string monsterKind;
            string numInRoomDice; // = "compiler complains if not set";
            MonsterStats monsterType = null;

            if (monsterToAdd == "GEN")
            {
                bool rerollMonster = true;
                while (rerollMonster)
                {
                    monsterKind = (string)Game.RandomArrayValue(MonsterKinds.ToArray());
                    rerollMonster = false;

                    monsterType = _fiendFolio[monsterKind];
                    
                    if (DungeonLevel < monsterType.MinLevel || DungeonLevel > monsterType.MaxLevel)
                    {
                        rerollMonster = true;
                    }
                    // EncounterRarity is percent chance to find relative to most common
                    // monsters on a level. Check it.
                    if (!(monsterType.EncounterRarity == 0))
                    {
                        int rarityRoll = Dice.Roll("1d100");
                        if (monsterType.EncounterRarity <= rarityRoll)
                        {
                            rerollMonster = true;
                        }
                    }
                }
            }
            else
            {
                monsterType = _fiendFolio[monsterToAdd];
            }

            if (monsterNumAppearing == "GEN")
            {
                numInRoomDice = monsterType.NumberAppearing;
            }
            else
            {
                numInRoomDice = monsterNumAppearing;
            }
            int numberOfMonsters = Dice.Roll(numInRoomDice);

            //monsterType = _fiendFolio["goblin"];

            for (int i = 0; i < numberOfMonsters; i++)
            {

                Monster monster = CreateMonster(monsterType);
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

        public Monster CreateMonster(MonsterStats monsterType)
        {
            Monster monster = new Monster()
            {
                Attack = Dice.Roll(monsterType.Attack),
                AttackChance = Dice.Roll(monsterType.AttackChance),
                AttackSkill = Dice.Roll(monsterType.AttackChance) / 10,
                MissileAttack = monsterType.MissileAttack,
                MissileRange = monsterType.MissileRange,
                MissileType = monsterType.MissileType,
                SpecialAttackRange = monsterType.SpecialAttackRange,
                SpecialAttackType = monsterType.SpecialAttackType,
                Awareness = monsterType.Awareness,
                Color = Colors.ColorLookup(monsterType.Color),
                Defense = Dice.Roll(monsterType.Defense),
                DefenseChance = Dice.Roll(monsterType.DefenseChance),
                DefenseSkill = Dice.Roll(monsterType.DefenseChance) / 10,
                Gold = Dice.Roll(monsterType.Gold),
                MaxHealth = Dice.Roll(monsterType.MaxHealth),
                Name = monsterType.Name,
                Speed = monsterType.Speed,
                Symbol = monsterType.Symbol,
                SAFerocious = monsterType.HasSpecialAbility("Ferocious"),
                SALifedrainOnHit = monsterType.HasSpecialAbility("Life Drain On Hit"),
                SALifedrainOnDamage = monsterType.HasSpecialAbility("Life Drain On Damage"),
                SARegeneration = monsterType.HasSpecialAbility("Regeneration"),
                SASenseThoughts = monsterType.HasSpecialAbility("Sense Thoughts"),
                SAStealthy = monsterType.HasSpecialAbility("Stealthy"),
                SAVampiric = monsterType.HasSpecialAbility("Vampiric"),
                SAVenomous = monsterType.HasSpecialAbility("Venomous"),
                SACausesStun = monsterType.HasSpecialAbility("Stuns"),
                SADoppelganger = monsterType.HasSpecialAbility("Doppelganger"),
                SAHighImpact = monsterType.HasSpecialAbility("High Impact"),
                IsUndead = monsterType.HasSpecialAbility("Undead"),
                IsImmobile = monsterType.HasSpecialAbility("Immobile")
            };
            monster.Health = monster.MaxHealth;
            
            
                
                

            return monster;
        }
    }
}
