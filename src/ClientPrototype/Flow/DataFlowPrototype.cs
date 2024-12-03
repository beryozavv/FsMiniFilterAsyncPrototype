using System.Text;
using System.Threading.Tasks.Dataflow;
using ClientPrototype.Abstractions;
using ClientPrototype.Dto;
using ClientPrototype.Exceptions;
using ClientPrototype.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClientPrototype.Flow;

internal class DataFlowPrototype : INotificationFlow
{
    private readonly ILogger<DataFlowPrototype> _logger;
    private readonly DriverSettings _driverSettings;
    private BufferBlock<DataFlowCommand> _bufferBlock = null!;
    private readonly IDriverClient _driverClient;
    private ActionBlock<ServerEvent> _serverEventBlock = null!;
    private ActionBlock<RequestNotification> _failWithReplyBlock = null!;
    private CancellationToken _cancellationToken;

    public DataFlowPrototype(IDriverClient driverClient, ILogger<DataFlowPrototype> logger,
        IOptions<DriverSettings> driverSettings)
    {
        _driverClient = driverClient;
        _logger = logger;
        _driverSettings = driverSettings.Value;
    }

    public void InitFlow(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;

        _bufferBlock = new(new ExecutionDataflowBlockOptions { CancellationToken = _cancellationToken });
        TransformBlock<DataFlowCommand, RequestNotification> getNotificationBlock = new(GetNotification,
            new ExecutionDataflowBlockOptions
            {
                CancellationToken = _cancellationToken, MaxDegreeOfParallelism = _driverSettings.MaxDegreeOfParallelism,
                EnsureOrdered = false
            });
        TransformBlock<RequestNotification, ServerEvent> processBlock = new(ProcessRequest,
            new ExecutionDataflowBlockOptions
            {
                CancellationToken = _cancellationToken, MaxDegreeOfParallelism = _driverSettings.MaxDegreeOfParallelism,
                EnsureOrdered = false
            });
        _failWithReplyBlock = new ActionBlock<RequestNotification>(FailWithReplyAsync,
            new ExecutionDataflowBlockOptions
            {
                CancellationToken = _cancellationToken, MaxDegreeOfParallelism = _driverSettings.MaxDegreeOfParallelism,
                EnsureOrdered = false
            });
        _serverEventBlock = new(SendEventToServer,
            new ExecutionDataflowBlockOptions
            {
                CancellationToken = _cancellationToken, MaxDegreeOfParallelism = _driverSettings.MaxDegreeOfParallelism,
                EnsureOrdered = false
            });

        _bufferBlock.LinkTo(getNotificationBlock, new()
        {
            PropagateCompletion = true
        });

        getNotificationBlock.LinkTo(processBlock, new DataflowLinkOptions
        {
            PropagateCompletion = true
        }, notification => notification.IsSuccess);

        getNotificationBlock.LinkTo(_failWithReplyBlock, new DataflowLinkOptions
        {
            PropagateCompletion = true
        }, notification => !notification.IsSuccess);

        processBlock.LinkTo(_serverEventBlock, new()
        {
            PropagateCompletion = true
        });
    }

    public async Task PostAsync(DataFlowCommand command, CancellationToken token)
    {
        try
        {
            await _bufferBlock.SendAsync(command, token);
            _logger.LogInformation("Command {Command} sent to buffer", command);
        }
        catch (Exception e)
        {
            // фатальная ошибка
            throw new DriverClientException("Sending DF command to buffer failed ", e);
        }
    }

    public async Task CompleteFlow()
    {
        _bufferBlock.Complete();
        await Task.WhenAll(_serverEventBlock.Completion, _failWithReplyBlock.Completion);
        _logger.LogInformation("Data flow completed");
    }

    private async Task FailWithReplyAsync(RequestNotification notification)
    {
        // todo
        // rights из опций?
        byte rights = 0;
        try
        {
            await ReplyAndPostNext(notification.CommandId, notification.MessageId, rights);
            _logger.LogWarning("notification failed {RequestCommandId}, {RequestMessageId}, rights={Rights}",
                notification.CommandId,
                notification.MessageId, rights);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "notification failed {RequestCommandId}, {RequestMessageId}, rights={Rights}",
                notification.CommandId, notification.MessageId, rights);
            throw; //todo
            // завершение работы флоу
        }
    }

    private async Task<RequestNotification> GetNotification(DataFlowCommand command)
    {
        RequestNotification? message = null;
        try
        {
            message = await _driverClient.ReadNotificationAsync(command.Id, _cancellationToken);
            // todo
            // эмуляция ошибки в contentString.Substring(0, message.ContentSize) когда в файле есть русский текст
            var contentString = Encoding.UTF8.GetString(message.Contents);
            _logger.LogInformation("Read CommandId: {CommandId} - msgId: {HeaderMessageId}; Content: {Substring}",
                command.Id, message.MessageId, contentString.Substring(0, 50));
            return message;
        }
        catch (DriverClientException)
        {
            throw; //todo
            // завершение работы флоу
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while getting notification Command = {CommandId}, Message = {Message}", command.Id, message?.MessageId);
            message!.IsSuccess = false;
            return message;
        }
    }

    private async Task<ServerEvent> ProcessRequest(RequestNotification notification)
    {
        // При получении MarkReaderMessage выполняет проверки, отправляет Reply драйверу, 
        // отправляет новую Command в буфер, формирует Event для сервера

        int rights;
        var serverEvent = new ServerEvent(notification.CommandId);
        try
        {
            _logger.LogInformation("Processing request CommandId={CommandId}, MessageId={MsgId}",
                notification.CommandId,
                notification.MessageId);

            // todo 
            // здесь выполняем всю логику проверок
            rights = notification.Contents[0] == 78 && notification.Contents[1] == 79 ? 0 : 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error before replying Command = {CommandId}, Message = {Message}",
                notification.CommandId, notification.MessageId);

            serverEvent.IsSuccess = false;
            serverEvent.NeedSendEvent = false;
            rights = 0; // todo дефолтный из опций
        }

        try
        {
            await ReplyAndPostNext(notification.CommandId, notification.MessageId, rights);

            return serverEvent;
        }
        catch (DriverClientException)
        {
            throw; //todo
            // завершение работы флоу
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error after replying Command = {CommandId}, Message = {Message}",
                notification.CommandId, notification.MessageId);
            serverEvent.IsSuccess = false;
            serverEvent.NeedSendEvent = true;
            return serverEvent;
        }
    }

    /// <summary>
    /// Вызов PostAsync всегда должен следовать за Reply
    /// </summary>
    /// <param name="commandId"></param>
    /// <param name="messageId"></param>
    /// <param name="rights"></param>
    private async Task ReplyAndPostNext(Guid commandId, ulong messageId, int rights)
    {
        var reply = new ReplyNotification(commandId, messageId, (byte)rights);
        var replyResult = _driverClient.Reply(reply);
        
        _logger.LogInformation(
            "Message {ReplyHeaderMessageId} Reply status = {ReplyResult}; Rights={ReplyDtoRights};",
            reply.MessageId, replyResult, reply.Rights);

        await PostAsync(new DataFlowCommand { Id = Guid.NewGuid(), Timestamp = DateTime.UtcNow },
            _cancellationToken);
    }

    private void SendEventToServer(ServerEvent serverEvent)
    {
        try
        {
            if (serverEvent.NeedSendEvent)
            {
                _logger.LogInformation("Sending event to server {Event}", serverEvent);
            }
            else
            {
                _logger.LogInformation("Skip of sending event to server {Event}", serverEvent);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error Sending Event To Server {Event}", serverEvent);
        }
    }
}