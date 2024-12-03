namespace ClientPrototype.Dto;

public record DataFlowCommand
{
    public Guid Id { get; set; }
    
    public DateTime Timestamp { get; set; }
}