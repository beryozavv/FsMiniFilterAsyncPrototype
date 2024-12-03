using ClientPrototype.Dto;

namespace ClientPrototype.Abstractions;

public interface INotificationFlow
{
    public void InitFlow(CancellationToken cancellationToken);
    Task PostAsync(DataFlowCommand command, CancellationToken token);
    Task CompleteFlow();
}
