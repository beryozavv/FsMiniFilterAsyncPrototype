namespace ClientPrototype.Dto;

public record RequestNotification : BaseDto
{
    public int ContentSize { get; init; }
    public byte[] Contents { get; } = null!;

    public RequestNotification(Guid commandId): base(commandId)
    {
        
    }

    public RequestNotification(Guid commandId, ulong messageId, byte[] contents, int contentSize):base(commandId, messageId)
    {
        Contents = contents;
        ContentSize = contentSize;
    }
}
