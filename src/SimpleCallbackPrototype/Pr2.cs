// using System;
// using System.Runtime.InteropServices;
// using System.Threading;
//
// class Program
// {
//     private static IntPtr hEvent;
//
//     [DllImport("kernel32.dll", SetLastError = true)]
//     private static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);
//
//     static void Main(string[] args)
//     {
//         // Создание события
//         hEvent = CreateEvent(IntPtr.Zero, false, false, null);
//         if (hEvent == IntPtr.Zero)
//         {
//             throw new Exception("Не удалось создать событие.");
//         }
//
//         // Регистрация ожидания события
//         RegisteredWaitHandle registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(
//             new WaitHandleSafe(hEvent),
//             new WaitOrTimerCallback(WaitCallback),
//             registeredWaitHandle, // Передаем RegisteredWaitHandle в коллбэк
//             Timeout.Infinite,
//             true
//         );
//
//         // Установка события (для демонстрации)
//         SetEvent(hEvent);
//
//         // Ожидание завершения
//         Console.WriteLine("Ожидание завершения...");
//         Console.ReadLine();
//     }
//
//     private static void WaitCallback(object state, bool timedOut)
//     {
//         Console.WriteLine("Событие сработало!");
//
//         // Отмена регистрации ожидания события
//         RegisteredWaitHandle registeredWaitHandle = (RegisteredWaitHandle)state;
//         registeredWaitHandle.Unregister(null);
//         Console.WriteLine("Регистрация отменена.");
//     }
//
//     private static void SetEvent(IntPtr eventHandle)
//     {
//         if (!kernel32.SetEvent(eventHandle))
//         {
//             throw new Exception("Не удалось установить событие.");
//         }
//     }
// }
//
// public class WaitHandleSafe : WaitHandle
// {
//     public WaitHandleSafe(IntPtr handle)
//     {
//         SafeWaitHandle = new Microsoft.Win32.SafeHandles.SafeWaitHandle(handle, false);
//     }
// }
//
// public static class kernel32
// {
//     [DllImport("kernel32.dll", SetLastError = true)]
//     public static extern bool SetEvent(IntPtr hEvent);