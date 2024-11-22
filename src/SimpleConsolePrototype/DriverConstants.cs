namespace SimpleConsolePrototype;

internal class DriverConstants
{
    public const int MarkReaderReadBufferSize = 1024;
    
    public const uint Ok = 0;

    public const uint ErrorInvalidHandle = 0x80070006;

    public const uint ErrorOperationAborted = 0x800703E3;

    public const uint ErrorIoPending = 0x800703E5;

    public const uint WaitTimeout = 0x80070102;

    public const uint ErrorAlreadyExists = 0x800700B7;

    public const uint ErrorFileNotFound = 0x80070002;

    public const uint ErrorServiceAlreadyRunning = 0x80070420;

    public const uint ErrorBadExeFormat = 0x800700C1;

    public const uint ErrorBadDriver = 0x800707D1;

    public const uint ErrorInvalidImageHash = 0x80070241;

    public const uint ErrorFltInstanceAltitudeCollision = 0x801F0011;

    public const uint ErrorFltInstanceNameCollision = 0x801F0012;

    public const uint ErrorFltFilterNotFound = 0x801F0013;

    public const uint ErrorFltInstanceNotFound = 0x801F0015;

    public const uint ErrorNotFound = 0x80070490;

    public const uint ErrorNoMoreItems = 0x80070103;

    public const uint ErrorInsufficientBuffer = 0x8007007A;
}
