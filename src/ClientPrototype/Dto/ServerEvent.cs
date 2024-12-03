namespace ClientPrototype.Dto;

internal record ServerEvent(Guid CommandId):BaseDto(CommandId)
{
    public bool NeedSendEvent { get; set; } = true;
}