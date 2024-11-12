using System.Runtime.InteropServices;

namespace ClientPrototype.Dto;

[StructLayout(LayoutKind.Sequential)]
public struct MarkReaderReply
{
    /// <summary>
    /// Есть ли право на просмотр документа
    /// </summary>
    /// <remarks>true если доступ есть, false - если доступа нет</remarks>
    public byte Rights;

    public MarkReaderReply(byte rights)
    {
        Rights = rights;
    }
}
