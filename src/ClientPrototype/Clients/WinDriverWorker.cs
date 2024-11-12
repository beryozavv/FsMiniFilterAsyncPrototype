using ClientPrototype.Abstractions;
using ClientPrototype.Dto;

namespace ClientPrototype.Clients;

internal class WinDriverWorker : IDriverWorker
{
    private readonly IDriverClient _driverClient;
    private readonly INotificationFlow _dataFlowPrototype;

    public WinDriverWorker(IDriverClient driverClient, INotificationFlow dataFlowPrototype)
    {
        _driverClient = driverClient;
        _dataFlowPrototype = dataFlowPrototype;
    }

    public async Task Watch(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                MarkReaderNotification readerNotification = _driverClient.ReadNotification();
                await _dataFlowPrototype.PostAsync(readerNotification);
            }
        }
        finally
        {
            _dataFlowPrototype.Complete();
            await Stop(cancellationToken);
        }
    }

    public Task Stop(CancellationToken cancellationToken)
    {
        _driverClient.Disconnect(cancellationToken);
        return Task.CompletedTask;
    }
}
