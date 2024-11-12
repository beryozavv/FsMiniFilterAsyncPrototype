using ClientPrototype.Abstractions;
using ClientPrototype.Dto;
using ClientPrototype.Flow;
using Moq;
using Xunit.Abstractions;

namespace ClientPrototype.Test;

public class DataFlowTest
{
    private readonly ITestOutputHelper _output;

    public DataFlowTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Test1()
    {
        _output.WriteLine("Test1");

        Mock<IDriverClient> messageManager = new();
        messageManager.Setup(mm => mm.ReadNotification())
            .Returns(() =>
            {
                _output.WriteLine("Buffer block");
                return new()
                {
                    Contents = Array.Empty<byte>(),
                    Reserved = 0,
                    Size = 0
                };
            });
        messageManager.Setup(mm => mm.Reply(It.IsAny<MarkReaderReply>()))
            .Returns(() =>
            {
                _output.WriteLine("Reply");
                return 1;
            });


        var flow = new DataFlowPrototype(messageManager.Object);

        for (int i = 0; i < 10; i++)
        {
            await flow.PostAsync(new()
            {
                Contents = Array.Empty<byte>(),
                Reserved = 0,
                Size = 0
            });
        }

        flow.Complete();

        Assert.True(true);
    }
}
