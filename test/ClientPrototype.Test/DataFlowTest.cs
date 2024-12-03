// using ClientPrototype.Abstractions;
// using ClientPrototype.Dto;
// using ClientPrototype.Flow;
// using Microsoft.Extensions.Logging;
// using Moq;
// using Xunit.Abstractions;
//
// namespace ClientPrototype.Test;
//
// public class DataFlowTest
// {
//     private readonly ITestOutputHelper _output;
//
//     public DataFlowTest(ITestOutputHelper output)
//     {
//         _output = output;
//     }
//
//     [Fact]
//     public async Task Test1()
//     {
//         _output.WriteLine("Test1");
//
//         Mock<IDriverClient> messageManager = new();
//         messageManager.Setup(mm => mm.ReadNotificationAsync(CancellationToken.None))
//             .ReturnsAsync(() =>
//             {
//                 _output.WriteLine("Buffer block");
//                 return new RequestNotification(1, []);
//             });
//         messageManager.Setup(mm => mm.Reply(It.IsAny<ReplyNotification>()))
//             .Returns(() =>
//             {
//                 _output.WriteLine("Reply");
//                 return 1;
//             });
//
//
//         var flow = new DataFlowPrototype(messageManager.Object, Mock.Of<ILogger<DataFlowPrototype>>());
//
//         for (int i = 0; i < 10; i++)
//         {
//             await flow.PostAsync(new(1, []), CancellationToken.None);
//         }
//
//         flow.CompleteFlow();
//
//         Assert.True(true);
//     }
// }
