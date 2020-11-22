namespace RuneRogue.Interfaces
{
    public interface IScheduleable
    {
        // Time returns the number of clock ticks that should occur until the 
        // ISchedulable is activated again
        int Time { get; }
    }
}
