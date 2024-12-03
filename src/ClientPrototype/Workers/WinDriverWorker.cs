using ClientPrototype.Abstractions;
using ClientPrototype.Dto;
using ClientPrototype.Helpers;
using ClientPrototype.Settings;
using Microsoft.Extensions.Options;

namespace ClientPrototype.Workers;

internal class WinDriverWorker : IDriverWorker
{
    private readonly IDriverClient _driverClient;
    private readonly INotificationFlow _dataFlowPrototype;
    private readonly DriverSettings _driverSettings;

    public WinDriverWorker(IDriverClient driverClient, INotificationFlow dataFlowPrototype, IOptions<DriverSettings> driverSettings)
    {
        _driverClient = driverClient;
        _dataFlowPrototype = dataFlowPrototype;
        _driverSettings = driverSettings.Value;
    }

    public async Task Watch(CancellationToken cancellationToken)
    {
        try
        {
            _driverClient.Connect(); //todo
            
            _dataFlowPrototype.InitFlow(cancellationToken);

            for (int i = 0; i < _driverSettings.MaxDegreeOfParallelism; i++)
            {
                var command = new DataFlowCommand
                {
                    Id = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow
                };
                await _dataFlowPrototype.PostAsync(command, cancellationToken);
            }

            await cancellationToken.WaitHandle.WaitAsync();
        }
        finally
        {
            await _dataFlowPrototype.CompleteFlow();

            await Stop(cancellationToken);
        }
    }


    public Task Stop(CancellationToken cancellationToken)
    {
        _driverClient.Disconnect(cancellationToken);
        return Task.CompletedTask;
    }
}