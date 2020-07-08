using System;
using System.Collections.Generic;
using System.Linq;
using RogueSharp;
using RogueSharp.DiceNotation;
using RuneRogue.Core;
using RuneRogue.Monsters;

namespace RuneRogue.Systems
{
    public class MonsterGenerator
    {
        private readonly MonsterKind _monsterType;

        public MonsterGenerator(MonsterKind monsterKind)
        {
            _monsterType = monsterKind;
        }
        public Monster CreateMonster()
        {
            Monster monster;
            switch (_monsterType)
            {
                case MonsterKind.Beetle:
                    monster = Beetle.Create(Game.mapLevel);
                    break;
                case MonsterKind.Kobold:
                    monster = Kobold.Create(Game.mapLevel);
                    break;
                case MonsterKind.Dragon:
                    monster = Dragon.Create(Game.mapLevel);
                    break;
                case MonsterKind.BoneClaws:
                    monster = BoneClaws.Create(Game.mapLevel);
                    break;
                default:
                    monster = Beetle.Create(0);
                    monster.Symbol = '?';
                    monster.Name = "Unknown";
                    break;
            }
            return monster;
        }
    }
}
