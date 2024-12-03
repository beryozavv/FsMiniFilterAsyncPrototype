namespace ClientPrototype.Exceptions;

public class DriverClientException : Exception
{
    public DriverClientException(string message) : base(message)
    {
        
    }
    
    public DriverClientException(string message, Exception exception) : base(message, exception)
    {
        
    }
}