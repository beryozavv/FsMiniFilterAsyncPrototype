using System.Runtime.InteropServices;
using ClientPrototype.Constants;

namespace ClientPrototype.Dto;

[StructLayout(LayoutKind.Sequential)]
public struct MarkReaderNotification
{
    public uint Size;
    public uint Reserved; // для выравнивания структуры содержимого по четырем словам

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = DriverConstants.MarkReaderReadBufferSize)]
    public byte[] Contents;
}
