namespace ClientPrototype.Dto;

public class RequestNotification
{
    public ulong MessageId { get; }
    public byte[] Contents { get; }

    public RequestNotification(ulong messageId, byte[] contents)
    {
        MessageId = messageId;
        Contents = contents;
    }
}
