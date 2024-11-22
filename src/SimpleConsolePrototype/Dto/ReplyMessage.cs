using SimpleConsolePrototype.Enums;

namespace SimpleConsolePrototype.Dto;

/// <summary>
/// Ответ на сообщение от драйвера.
/// </summary>
public class ReplyMessage
{
    /// <summary>
    /// Номер сообщения.
    /// </summary>
    /// Выставляется драйвером. Должен иметь тот же номер, что и в сообщении от драйвера.
    public long MessageId { get; }

    /// <summary>
    /// Тип ответа для драйвера. Указание что делать с файлом.
    /// </summary>
    public eReplyActionType ReplyType { get; }

    /// <summary>
    /// DOS наименование диска.
    /// </summary>
    public string Volume { get; }

    /// <summary>
    /// Путь до файла.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Имя файла.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Полное имя файла.
    /// </summary>
    public string FullFilePath => $"{Volume}{Path}{FileName}";

    /// <summary>
    /// Ответ для драйвера линукс.
    /// </summary>
    public string FullMessageLinux
    {
        get
        {
            string replyAction = ReplyType == eReplyActionType.Deny
                ? "|Deny"
                : "|Allow";

            return $"{Path}{FileName}" + replyAction;
        }
    }

    /// <summary>
    /// Результат работы (NativeErrorCodes = коды ошибок API Win32).
    /// </summary>
    public uint HandlerResult { get; set; }

    /// <summary>
    /// Не добавлять в кеш.
    /// </summary>
    public bool NotAddToCache { get; set; }
    
    public ReplyMessage(
        eProcessingResult actionType, 
        long messageId,
        string volume,
        string path,
        string fileName)
    {
        SimpleConsolePrototype.Enums.eReplyActionType replyActionType = eReplyActionType.Allow;
        switch (actionType)
        {
            case eProcessingResult.Allow:
                replyActionType = eReplyActionType.Allow;
                break;

            case eProcessingResult.Deny:
                replyActionType = eReplyActionType.Deny;
                break;

            case eProcessingResult.AppIsNotSupported:
                replyActionType = eReplyActionType.Deny;
                break;

            case eProcessingResult.AppIsNotTrusted:
                replyActionType = eReplyActionType.Deny;
                break;
        }

        MessageId = messageId;
        ReplyType = replyActionType;
        Volume = volume;
        Path = path;
        FileName = fileName;
    }

    public ReplyMessage(
        long messageId,
        eReplyActionType replyType,
        string volume,
        string path,
        string fileName)
    {
        MessageId = messageId;
        ReplyType = replyType;
        Volume = volume;
        Path = path;
        FileName = fileName;
    }

    

    /// <summary>
    /// Преобразование текущей информации полей класса в строку.
    /// </summary>
    public override string ToString()
    {
        return
            $"MsgId: <{MessageId}>, ReplyType: <{ReplyType}>, Vol: <{Volume}>, Path: <{Path}>, FileName: <{FileName}>, NotAddToCache: <{NotAddToCache}>";
    }
}
