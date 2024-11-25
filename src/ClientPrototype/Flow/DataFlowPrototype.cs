using System.Text;
using System.Threading.Tasks.Dataflow;
using ClientPrototype.Abstractions;
using ClientPrototype.Dto;
using Microsoft.Extensions.Logging;

namespace ClientPrototype.Flow;

internal class DataFlowPrototype : INotificationFlow
{
    private readonly ILogger<DataFlowPrototype> _logger;
    private readonly BufferBlock<RequestNotification> _bufferBlock;
    private readonly TransformBlock<RequestNotification, ReplyNotification> _transformBlock;
    private readonly ActionBlock<ReplyNotification> _responseBlock;
    private readonly IDriverClient _driverClient;

    public DataFlowPrototype(IDriverClient driverClient, ILogger<DataFlowPrototype> logger)
    {
        _driverClient = driverClient;
        _logger = logger;
        _bufferBlock = new();
        _transformBlock = new(ProcessRequest);
        _responseBlock = new(SendResponseToDriver);

        _bufferBlock.LinkTo(_transformBlock, new()
        {
            PropagateCompletion = true
        });
        _transformBlock.LinkTo(_responseBlock, new()
        {
            PropagateCompletion = true
        });
    }

    public async Task PostAsync(RequestNotification request, CancellationToken token)
    {
        _logger.LogInformation("Posting mark reader.");
        await _bufferBlock.SendAsync(request, token);
    }

    public void Complete() => _bufferBlock.Complete();

    private ReplyNotification ProcessRequest(RequestNotification notification)
    {
        _logger.LogInformation("Processing mark reader");
        //var r = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, notification.Contents);
        var stringBuffer = Encoding.ASCII.GetString(notification.Contents);
        
        _logger.LogInformation("Content from buffer: {message}", stringBuffer);

        return new(notification.MessageId, 0);
    }

    private void SendResponseToDriver(ReplyNotification reply)
    {
        _logger.LogInformation("Sending reply: rights - {reply}", reply.Rights);
        try
        {
            _driverClient.Reply(reply);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to send reply");
        }
    }
}
