namespace RuneRogue.Core
{
    // Direction values correspond to numpad numbers
    public enum MonsterKind
    {
        Kobold = 0,
        Beetle = 1,
        Dragon = 2,
        BoneClaws = 3
    }

    public class MonsterStats
    {
        public MonsterKind Kind { get; set; }
        public string Attack { get; set; }
        public string AttackChance { get; set; }
        public int Awareness { get; set; }
        public RLNET.RLColor Color { get; set; }
        public string Defense { get; set; }
        public string DefenseChance { get; set; }
        public string Gold { get; set; }
        //public int Health { get; set; }
        public string MaxHealth { get; set; }
        public string Name { get; set; }
        public int Speed { get; set; }
        public char Symbol { get; set; }
        public string NumberAppearing { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
    }

    public class MonsterData
    {
        public MonsterStats[] _monsterData;

        public void ReadMonsterData()
        {
            _monsterData = new MonsterStats[]
            {
                new MonsterStats()
                {
                    Kind=MonsterKind.Beetle,
                    Attack = "1D2",
                    AttackChance = "25D2",
                    Awareness = 10,
                    Color = Colors.BeetleColor,
                    Defense =  "2D2",
                    DefenseChance = "8D4",
                    Gold = "0",
                    MaxHealth = "D4",
                    Name = "Giant Beetle",
                    Speed = 7,
                    Symbol = 'b',
                    NumberAppearing = "d3+d6-1",
                    MinLevel = 1,
                    MaxLevel = 6
                },
                new MonsterStats()
                {
                    Kind=MonsterKind.BoneClaws,
                    Attack = "2D3" ,
                    AttackChance = "25D4",
                    Awareness = 10,
                    Color = Colors.BoneClawsColor,
                    Defense = "3D2" ,
                    DefenseChance =  "15D3" ,
                    Gold = "5D5",
                    MaxHealth = "1D3",
                    Name = "Bone Claws",
                    Speed = 12,
                    Symbol = 'b',
                    NumberAppearing = "5-2d4k1",
                    MinLevel = 1,
                    MaxLevel = 5
                },
                new MonsterStats()
                {
                    Kind = MonsterKind.Dragon,
                    Attack = "5D2",
                    AttackChance = "25D3+20",
                    Awareness = 10,
                    Color = Colors.DragonColor,
                    Defense = "3d3k2",
                    DefenseChance = "8D8",
                    Gold = "10D20",
                    MaxHealth = "6D6",
                    Name = "Dragon",
                    Speed = 14,
                    Symbol = 'D',
                    NumberAppearing = "1",
                    MinLevel = 2,
                    MaxLevel = 10
                },
                new MonsterStats()
                {
                    Kind = MonsterKind.Kobold,
                    Attack = "1D3",
                    AttackChance = "25D3",
                    Awareness = 10,
                    Color = Colors.KoboldColor,
                    Defense = "1D3",
                    DefenseChance = "10D4",
                    Gold = "5D5",
                    MaxHealth = "2D5",
                    Name = "Kobold",
                    Speed = 14,
                    Symbol = 'k',
                    NumberAppearing = "5-2d4k1",
                    MinLevel = 1,
                    MaxLevel = 3
                }
            };
        }
    }
}
