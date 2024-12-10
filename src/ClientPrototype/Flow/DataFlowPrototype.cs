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
    private CancellationTokenSource _cts = null!;
    private CancellationToken _cancellationToken;

    public DataFlowPrototype(IDriverClient driverClient, ILogger<DataFlowPrototype> logger,
        IOptions<DriverSettings> driverSettings)
    {
        _driverClient = driverClient;
        _logger = logger;
        _driverSettings = driverSettings.Value;
    }

    public void InitFlow(CancellationTokenSource cts)
    {
        _cancellationToken = cts.Token;
        _cts = cts;
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
            _logger.LogTrace("Command {Command} sent to buffer", command);
        }
        catch (Exception e)
        {
            //todo
            // может здесь сразу cделать cts.Cancel?
            // фатальная ошибка
            throw new CriticalDriverCommunicationException("Sending DF command to buffer failed ", e);
        }
    }

    public async Task CompleteFlow()
    {
        try
        {
            _bufferBlock.Complete();
            // здесь обязательно дожидаемся завершения всех терминальных блоков 
            await Task.WhenAll(_serverEventBlock.Completion, _failWithReplyBlock.Completion);
            _logger.LogInformation("Dataflow completed");
        }
        catch (AggregateException ex)
        {
            foreach (var innerException in ex.Flatten().InnerExceptions)
            {
                _logger.LogCritical(innerException, "DataFlow completed with error");
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "DataFlow encountered an unexpected error");
        }
    }

    private async Task FailWithReplyAsync(RequestNotification notification)
    {
        int rights = _driverSettings.DefaultRights;
        try
        {
            await ReplyAndPostNext(notification.CommandId, notification.MessageId, rights);
            _logger.LogWarning("notification failed {RequestCommandId}, {RequestMessageId}, rights={Rights}",
                notification.CommandId,
                notification.MessageId, rights);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "notification failed {RequestCommandId}, {RequestMessageId}, rights={Rights}",
                notification.CommandId, notification.MessageId, rights);
            // завершение работы флоу
            await _cts.CancelAsync();
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
            _logger.LogTrace("Read CommandId: {CommandId} - msgId: {HeaderMessageId}; Content: {Substring}",
                command.Id, message.MessageId, contentString.Substring(0, 50));
            return message;
        }
        catch (CriticalDriverCommunicationException ex)
        {
            if (ex.InnerException is TaskCanceledException)
            {
                _logger.LogError(ex, "GetNotification canceled {RequestCommandId}, {MessageId}",
                    command.Id, message?.MessageId);
            }
            else
            {
                _logger.LogCritical(ex, "GetNotification failed {RequestCommandId}, {MessageId}",
                    command.Id, message?.MessageId);
            }

            // завершение работы флоу
            await _cts.CancelAsync();
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while getting notification Command = {CommandId}, Message = {MessageId}",
                command.Id, message?.MessageId);
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
            _logger.LogTrace("Processing request CommandId={CommandId}, MessageId={MsgId}",
                notification.CommandId,
                notification.MessageId);

            // todo 
            // здесь выполняем всю логику проверок
            rights = await CustomChecks(notification, _cancellationToken)
                .WaitAsync(_driverSettings.CustomChecksTimeout, _cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error before replying Command = {CommandId}, Message = {Message}",
                notification.CommandId, notification.MessageId);

            serverEvent.IsSuccess = false;
            serverEvent.NeedSendEvent = false;
            rights = _driverSettings.DefaultRights;
        }

        try
        {
            await ReplyAndPostNext(notification.CommandId, notification.MessageId, rights);

            return serverEvent;
        }
        catch (CriticalDriverCommunicationException ex)
        {
            _logger.LogCritical(ex, "notification failed {RequestCommandId}, {RequestMessageId}, rights={Rights}",
                notification.CommandId, notification.MessageId, rights);
            // завершение работы флоу
            await _cts.CancelAsync();
            throw;
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
    /// Здесь выполняем все необходимые проверки файла и обращения во внешние API
    /// </summary>
    /// <param name="notification"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<int> CustomChecks(RequestNotification notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rights = notification.Contents[0] == 78 && notification.Contents[1] == 79 ? 0 : 1;
        return await Task.FromResult(rights);
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
        
        _logger.LogTrace(
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
                _logger.LogTrace("Sending event to server {Event}", serverEvent);
            }
            else
            {
                _logger.LogTrace("Skip of sending event to server {Event}", serverEvent);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error Sending Event To Server {Event}", serverEvent);
        }
    }
}