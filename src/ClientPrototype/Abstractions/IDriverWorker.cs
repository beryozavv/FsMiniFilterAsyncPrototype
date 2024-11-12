namespace ClientPrototype.Abstractions;

public interface IDriverWorker
{
    /// <summary>
    /// Начать обработку сообщений от драйвера
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Watch(CancellationToken cancellationToken);
    
    Task Stop(CancellationToken cancellationToken);
}
