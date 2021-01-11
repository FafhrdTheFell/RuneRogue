namespace RuneRogue.Interfaces
{
    public interface IActor
    {
        int Attack { get; set; }
        int WeaponSkill { get; set; }
        int Awareness { get; set; }
        int Armor { get; set; }
        int DodgeSkill { get; set; }
        int Gold { get; set; }
        int Health { get; set; }
        int MaxHealth { get; set; }
        string Name { get; set; }
        int Speed { get; set; }
    }
}
