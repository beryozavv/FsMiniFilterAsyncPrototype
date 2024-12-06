using ClientPrototype.Dto;

namespace ClientPrototype.Abstractions;

public interface IDriverClient
{
    /// <summary>
    /// Подключение к драйверу
    /// </summary>
    void Connect();
    
    /// <summary>
    /// Получить сообщение из асинхронной очереди
    /// </summary>
    /// <returns></returns>
    Task<RequestNotification> ReadNotificationAsync(Guid commandId, CancellationToken cancellationToken);

    /// <summary>
    /// Ответить драйверу
    /// </summary>
    /// <param name="replyDto">Ответ драйверу</param>
    /// <returns></returns>
    uint Reply(ReplyNotification replyDto);

    void Disconnect();
}
