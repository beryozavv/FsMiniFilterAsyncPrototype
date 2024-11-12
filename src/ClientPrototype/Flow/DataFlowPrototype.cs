using System.Threading.Tasks.Dataflow;
using ClientPrototype.Abstractions;
using ClientPrototype.Dto;

namespace ClientPrototype.Flow;

internal class DataFlowPrototype : INotificationFlow
{
    private readonly BufferBlock<MarkReaderNotification> _bufferBlock;
    private readonly TransformBlock<MarkReaderNotification, MarkReaderReply> _transformBlock;
    private readonly ActionBlock<MarkReaderReply> _responseBlock;
    private readonly IDriverClient _driverClient;

    public DataFlowPrototype(IDriverClient driverClient)
    {
        _driverClient = driverClient;
        _bufferBlock = new()
        {
            
        };
        _transformBlock = new(ProcessRequest);
        _responseBlock = new(SendResponseToDriver);

        _bufferBlock.LinkTo(_transformBlock, new()
        {
            PropagateCompletion = true
        });
        _transformBlock.LinkTo(_responseBlock);
    }

    public async Task PostAsync(MarkReaderNotification request)
    {
        await _bufferBlock.SendAsync(request);
    }
    
    public void Complete() => _bufferBlock.Complete();

    private MarkReaderReply ProcessRequest(MarkReaderNotification notification)
    {
        var random = new Random();
        var processResult = random.NextInt64(0, 2);
        var rights = processResult > 0 ? (byte)0 : (byte)1;

        return new(rights);
    }

    private void SendResponseToDriver(MarkReaderReply reply)
    {
        _driverClient.Reply(reply);
    }
}
