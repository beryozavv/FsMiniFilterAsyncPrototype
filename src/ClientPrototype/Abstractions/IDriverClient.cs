using ClientPrototype.Dto;

namespace ClientPrototype.Abstractions;

public interface IDriverClient
{
    /// <summary>
    /// Получить сообщение от драйвера
    /// </summary>
    /// <returns></returns>
    MarkReaderNotification ReadNotification();

    /// <summary>
    /// Ответить драйверу
    /// </summary>
    /// <param name="reply"></param>
    /// <returns></returns>
    uint Reply(MarkReaderReply reply);

    void Disconnect(CancellationToken token);
}
