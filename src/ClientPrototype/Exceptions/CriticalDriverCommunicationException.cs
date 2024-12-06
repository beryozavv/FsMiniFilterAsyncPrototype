namespace ClientPrototype.Exceptions;

/// <summary>
/// Критическая ошибка, которая требует перезапуск воркера для коммуникации с драйвером
/// </summary>
public class CriticalDriverCommunicationException : Exception
{
    public CriticalDriverCommunicationException(string message) : base(message)
    {
        
    }
    
    public CriticalDriverCommunicationException(string message, Exception exception) : base(message, exception)
    {
        
    }
}