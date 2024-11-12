using ClientPrototype.Dto;

namespace ClientPrototype.Abstractions;

public interface INotificationFlow
{
    Task PostAsync(MarkReaderNotification request);
    void Complete();
}
