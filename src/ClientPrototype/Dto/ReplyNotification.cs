namespace ClientPrototype.Dto;

public record ReplyNotification : BaseDto
{
    public uint Status { get; }
    public byte Rights { get; }

    public ReplyNotification(Guid commandId, ulong messageId, byte rights) : this(commandId, messageId, 0, rights)
    {
    }

    public ReplyNotification(Guid commandId, ulong messageId, uint status, byte rights):base(commandId, messageId)
    {
        MessageId = messageId;
        Status = status;
        Rights = rights;
    }
}
