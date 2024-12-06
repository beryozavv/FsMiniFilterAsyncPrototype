namespace ClientPrototype.Abstractions;

public interface IDriverWorker
{
    /// <summary>
    /// Начать обработку сообщений от драйвера
    /// </summary>
    /// <returns></returns>
    Task Watch();

    /// <summary>
    /// Остановить обработку, отключиться от драйвера и выгрузить драйвер
    /// </summary>
    /// <returns></returns>
    Task Stop();
}
