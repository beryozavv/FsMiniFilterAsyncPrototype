namespace ClientPrototype.Dto;

public record BaseDto(Guid CommandId)
{
    public bool IsSuccess { get; set; } = true;
    public Guid CommandId { get; } = CommandId;
    public ulong MessageId { get; init; }

    public BaseDto(Guid commandId, ulong messageId) : this(commandId)
    {
        MessageId = messageId;
    }
}