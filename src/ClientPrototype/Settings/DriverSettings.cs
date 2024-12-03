namespace ClientPrototype.Settings;

internal class DriverSettings
{
    public required string ConnectionName { get; init; }
    public required string DriverName { get; init; }
    public int MaxDegreeOfParallelism { get; init; }
}