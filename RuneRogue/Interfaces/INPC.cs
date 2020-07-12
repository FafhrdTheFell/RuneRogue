namespace RuneRogue.Interfaces
{
    public interface INPC
    {
        string NumberAppearing { get; set; }
        int MinLevel { get; set; }
        int MaxLevel { get; set; }
        string[] FollowerKinds { get; set; }
        string[] FollowerNumberAppearing { get; set; }
        int[] FollowerProbability { get; set; }
    }
}
