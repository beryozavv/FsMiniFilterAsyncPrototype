using ClientPrototype.Dto;

namespace ClientPrototype.Abstractions;

public interface IDriverClient
{
    /// <summary>
    /// Прочитать сообщение от драйвера
    /// </summary>
    void ReadNotification();

    /// <summary>
    /// Получить сообщение из асинхронной очереди
    /// </summary>
    /// <returns></returns>
    RequestNotification ReadAsyncNotification();

    /// <summary>
    /// Ответить драйверу
    /// </summary>
    /// <param name="reply">Ответ драйверу</param>
    /// <returns></returns>
    uint Reply(ReplyNotification reply);

    void Disconnect(CancellationToken token);
}
