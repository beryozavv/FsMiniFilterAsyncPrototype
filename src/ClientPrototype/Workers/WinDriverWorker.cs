using ClientPrototype.Abstractions;
using ClientPrototype.Dto;

namespace ClientPrototype.Workers;

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
                // var readerNotification = _driverClient.ReadAsyncNotification();
                var readerNotification = _driverClient.ReadNotification();
                await _dataFlowPrototype.PostAsync(readerNotification, cancellationToken);
                
                _driverClient.ReadNotification();
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
