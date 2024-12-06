using ClientPrototype.Abstractions;
using ClientPrototype.Dto;
using ClientPrototype.Helpers;
using ClientPrototype.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClientPrototype.Workers;

internal class WinDriverWorker : IDriverWorker
{
    private readonly IDriverClient _driverClient;
    private readonly INotificationFlow _dataFlowPrototype;
    private readonly ILogger _logger;
    private readonly DriverSettings _driverSettings;

    public WinDriverWorker(IDriverClient driverClient, INotificationFlow dataFlowPrototype,
        IOptions<DriverSettings> driverSettings, ILogger<WinDriverWorker> logger)
    {
        _driverClient = driverClient;
        _dataFlowPrototype = dataFlowPrototype;
        _logger = logger;
        _driverSettings = driverSettings.Value;
    }

    public async Task Watch()
    {
        while (true)
        {
            using (var cts = new CancellationTokenSource())
            {
                try
                {
                    Start(cts);

                    await InitialFillingCommandBuffer(cts);

                    await cts.Token.WaitHandle.WaitAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, nameof(Watch));
                }
                finally
                {
                    await Stop();
                }
            }
        }
    }

    private async Task InitialFillingCommandBuffer(CancellationTokenSource cts)
    {
        for (int i = 0; i < _driverSettings.MaxDegreeOfParallelism; i++)
        {
            var command = new DataFlowCommand
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow
            };
            await _dataFlowPrototype.PostAsync(command, cts.Token);
        }
    }

    private void Start(CancellationTokenSource cts)
    {
        _driverClient.Connect();
        _dataFlowPrototype.InitFlow(cts);
    }

    public async Task Stop()
    {
        try
        {
            await _dataFlowPrototype.CompleteFlow();

            _driverClient.Disconnect();
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, nameof(Stop));
        }
    }
}