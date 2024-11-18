namespace ClientPrototype.Dto;

public class ReplyNotification
{
    public ulong MessageId { get; }
    public uint Status { get; }
    public byte Rights { get; }

    public ReplyNotification(ulong messageId, byte rights) : this(messageId, 0, rights)
    {
    }

    public ReplyNotification(ulong messageId, uint status, byte rights)
    {
        MessageId = messageId;
        Status = status;
        Rights = rights;
    }
}
