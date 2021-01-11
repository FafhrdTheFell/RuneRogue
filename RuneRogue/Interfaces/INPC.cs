namespace RuneRogue.Interfaces
{
    public interface INPC
    {
        string NumberAppearing { get; set; }
        string Role { get; set; }
        int MinLevel { get; set; }
        int MaxLevel { get; set; }
        string[] FollowerKinds { get; set; }
        string[] FollowerNumberAppearing { get; set; }
        int[] FollowerProbability { get; set; }
        int EncounterRarity { get; set; }
        int NumberKilled { get; set; }
        int NumberGenerated { get; set; }
        bool IsUnique { get; set; }
    }
}
