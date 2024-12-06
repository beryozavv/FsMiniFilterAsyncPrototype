using ClientPrototype.Dto;

namespace ClientPrototype.Abstractions;

public interface INotificationFlow
{
    public void InitFlow(CancellationTokenSource cts);
    Task PostAsync(DataFlowCommand command, CancellationToken token);
    Task CompleteFlow();
}
