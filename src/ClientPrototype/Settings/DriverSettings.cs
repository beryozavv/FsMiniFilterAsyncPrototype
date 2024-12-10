namespace ClientPrototype.Settings;

internal class DriverSettings
{
    public required string ConnectionName { get; init; }
    public required string DriverName { get; init; }
    public int MaxDegreeOfParallelism { get; init; }
    public int DefaultRights { get; init; }
    public TimeSpan CustomChecksTimeout { get; init; }
}