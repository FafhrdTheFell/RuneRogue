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
    class KindsNotUniqueException : Exception
    {
        public KindsNotUniqueException(string message)
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
            //_monsterManual = new MonsterStats[]
            //{
            //    new MonsterStats()
            //    {
            //        Kind=MonsterKind.Beetle,
            //        Attack = "1D2",
            //        AttackChance = "25D2",
            //        Awareness = 10,
            //        Color = Colors.BeetleColor,
            //        Defense =  "2D2",
            //        DefenseChance = "8D4",
            //        Gold = "0",
            //        MaxHealth = "D4",
            //        Name = "Giant Beetle",
            //        Speed = 7,
            //        Symbol = 'b',
            //        NumberAppearing = "d3+d6-1",
            //        MinLevel = 1,
            //        MaxLevel = 6
            //    },
            //    new MonsterStats()
            //    {
            //        Kind=MonsterKind.BoneClaws,
            //        Attack = "2D3" ,
            //        AttackChance = "25D4",
            //        Awareness = 10,
            //        Color = Colors.BoneClawsColor,
            //        Defense = "3D2" ,
            //        DefenseChance =  "15D3" ,
            //        Gold = "5D5",
            //        MaxHealth = "1D3",
            //        Name = "Bone Claws",
            //        Speed = 12,
            //        Symbol = 'b',
            //        NumberAppearing = "5-2d4k1",
            //        MinLevel = 1,
            //        MaxLevel = 5
            //    },
            //    new MonsterStats()
            //    {
            //        Kind = MonsterKind.Dragon,
            //        Attack = "5D2",
            //        AttackChance = "25D3+20",
            //        Awareness = 10,
            //        Color = Colors.DragonColor,
            //        Defense = "3d3k2",
            //        DefenseChance = "8D8",
            //        Gold = "10D20",
            //        MaxHealth = "6D6",
            //        Name = "Dragon",
            //        Speed = 14,
            //        Symbol = 'D',
            //        NumberAppearing = "1",
            //        MinLevel = 2,
            //        MaxLevel = 10
            //    },
            //    new MonsterStats()
            //    {
            //        Kind = MonsterKind.Kobold,
            //        Attack = "1D3",
            //        AttackChance = "25D3",
            //        Awareness = 10,
            //        Color = Colors.KoboldColor,
            //        Defense = "1D3",
            //        DefenseChance = "10D4",
            //        Gold = "5D5",
            //        MaxHealth = "2D5",
            //        Name = "Kobold",
            //        Speed = 14,
            //        Symbol = 'k',
            //        NumberAppearing = "5-2d4k1",
            //        MinLevel = 1,
            //        MaxLevel = 3
            //    }
            //};
            GenerateKinds();
            GenerateIndex();
            PrintMonsterString("doppelganger");
        }

        void WriteMonsterData(string FileName)
        {
            string jsonString = JsonSerializer.Serialize(_monsterManual, _jsonOptions);
            File.WriteAllText(FileName, jsonString);
        }

        void PrintMonsterString(string monster)
        {
            MonsterStats data = _monsterManual[_manualPage[monster]];
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
            }
            bool isUnique = monsterKinds.Distinct().Count() == monsterKinds.Count();
            if (!isUnique)
            {
                throw new KindsNotUniqueException($"Monster kinds values not unique: {monsterKinds.ToArray().ToString()}.");
            }
            _monsterKinds = monsterKinds.ToArray();
        }

        public Monster CreateMonster(string monsterKind)
        {
            Monster monster = new Monster();
            int page = _manualPage[monsterKind];
            monster.Attack = Dice.Roll(_monsterManual[page].Attack);
            monster.AttackChance = Dice.Roll(_monsterManual[page].AttackChance);
            monster.AttackSkill = monster.AttackChance / 10;
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
            monster.SALifedrainOnHit = _monsterManual[page].SALifedrainOnHit;
            monster.SALifedrainOnDamage = _monsterManual[page].SALifedrainOnDamage; 
            monster.SARegeneration = _monsterManual[page].SARegeneration;
            monster.SAVampiric = _monsterManual[page].SAVampiric;
            monster.SADoppelganger = _monsterManual[page].SADoppelganger;
            monster.SAHighImpact = _monsterManual[page].SAHighImpact; 

            return monster;
        }
    }
}
