using System.Runtime.InteropServices;
using System.Text;
using SimpleConsolePrototype.Dto;

namespace SimpleConsolePrototype.DriverMessage;

/// <summary>
/// Полное сообщение-ответ для драйвера Windows.
/// </summary>
internal class DriverFullReplyMessage
{
    /// <summary>
    /// Размер заголовка сообщения (в байтах).
    /// </summary>
    public readonly int HeaderSize = Marshal.SizeOf(typeof(DriverReplyHeader));

    /// <summary>
    /// Размер данных сообщения (в байтах).
    /// </summary>
    public readonly int DataSize = Marshal.SizeOf(typeof(DriverReplyData));

    /// <summary>
    /// Размер всего сообщения (в байтах).
    /// </summary>
    public int Size;

    /// <summary>
    /// Полное сообщение. Заголовок.
    /// </summary>
    public DriverReplyHeader Header;

    /// <summary>
    /// Полное сообщение. Данные.
    /// </summary>
    public DriverReplyData Data;

    /// <summary>
    /// Полное сообщение. Полное имя файла - Том (часть изменяемого размера).
    /// </summary>
    public byte[] VolumePartBytes;

    /// <summary>
    /// Полное сообщение. Полное имя файла - До папки, содержащей файл (часть изменяемого размера).
    /// </summary>
    public byte[] PathPartBytes;

    /// <summary>
    /// Полное сообщение. Полное имя файла - Файл (часть изменяемого размера).
    /// </summary>
    public byte[] FilePartBytes;

    /// <summary>
    /// Формирование полного сообщения.
    /// </summary>
    /// <param name="messageId">ID сообщения. Должно соответствовать ID сообщения от драйвера.</param>
    /// <param name="availableReplyMessageSize">макс. размер сообщения драйверу (получено в сообщении от драйвера).</param>
    /// <param name="replyMessage">Сообщение ответ (неполное).</param>
    /// <param name="logger">Логгер</param>
    public void Build(long messageId, long availableReplyMessageSize, ReplyMessage replyMessage)
    {
        VolumePartBytes = Encoding.Unicode.GetBytes(replyMessage.Volume + '\0');
        PathPartBytes = Encoding.Unicode.GetBytes(replyMessage.Path + '\0');
        FilePartBytes = Encoding.Unicode.GetBytes(replyMessage.FileName + '\0');

        Size = HeaderSize + DataSize + VolumePartBytes.Length + PathPartBytes.Length + FilePartBytes.Length;

        // Сравнить размер сообщения-ответа с размером, который может принять драйвер
        if (availableReplyMessageSize == 0)
        {
            Console.WriteLine("From kernel received available ReplyLength == 0.");
            replyMessage.HandlerResult = DriverConstants.ErrorInsufficientBuffer;
        }
        if (Size > availableReplyMessageSize)
        {
            Console.WriteLine(
                "Reply obj size is bigger then available length. Size: {Size}, AvailableLength: {AvailableSize}", Size,
                availableReplyMessageSize);
            replyMessage.HandlerResult = DriverConstants.ErrorInsufficientBuffer;
        }

        Header = new DriverReplyHeader
        {
            MessageId = messageId, Status = replyMessage.HandlerResult
        };

        try
        {
            Data = new DriverReplyData
            {
                ActionType = replyMessage.ReplyType,
                VolumePartLength = Convert.ToUInt16(VolumePartBytes.Length),
                PathPartLength = Convert.ToUInt16(PathPartBytes.Length),
                FilePartLength = Convert.ToUInt16(FilePartBytes.Length),
                NotAddToCache = Convert.ToByte(0)
            };
        }
        catch (OverflowException ex)
        {
            Console.WriteLine($"Name part array length is not Uint16: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Преобразование текущей информации полей класса в строку.
    /// </summary>
    public override string ToString()
    {
        return $"Full reply message:" +
               $" {nameof(Header.MessageId)}:{Header.MessageId}" +
               $" {nameof(Header.Status)}:{Header.Status}" +
               $" {nameof(Data.VolumePartLength)}:{Data.VolumePartLength}" +
               $" {nameof(Data.PathPartLength)}:{Data.PathPartLength}" +
               $" {nameof(Data.FilePartLength)}:{Data.FilePartLength}" +
               $" {nameof(Data.NotAddToCache)}:{Data.NotAddToCache}" +
               $" {nameof(VolumePartBytes)}:{Encoding.UTF8.GetString(VolumePartBytes, 0, VolumePartBytes.Length)}" +
               $" {nameof(PathPartBytes)}:{Encoding.UTF8.GetString(PathPartBytes, 0, PathPartBytes.Length)}" +
               $" {nameof(FilePartBytes)}:{Encoding.UTF8.GetString(FilePartBytes, 0, FilePartBytes.Length)}";
    }
}
