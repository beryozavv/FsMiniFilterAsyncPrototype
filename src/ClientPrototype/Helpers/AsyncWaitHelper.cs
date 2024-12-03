using System.Runtime.InteropServices;
using ClientPrototype.NativeWrappers;

namespace ClientPrototype.Helpers;

public static class AsyncWaitHelper
{
    /// <summary>
    /// Подписка на событие и асинхронное ожидание коллбэка
    /// </summary>
    /// <param name="waitHandle">Событие</param>
    public static async Task WaitAsync(this WaitHandle waitHandle)
    {
        var tcs = new TaskCompletionSource();
        var regWaitHandle =
            ThreadPool.RegisterWaitForSingleObject(waitHandle, (_, _) => tcs.SetResult(), null, -1, true);
        await tcs.Task;
        regWaitHandle.Unregister(null);
    }

    /// <summary>
    /// Подписка на событие и асинхронное ожидание коллбэка
    /// </summary>
    /// <param name="hEvent">Событие</param>
    /// <param name="messagePtr">Указатель на сообщение</param>
    /// <typeparam name="TResult">Тип сообщения</typeparam>
    /// <returns>Сообщение</returns>
    public static async Task<TResult> WaitForMessageAsync<TResult>(this IntPtr hEvent, IntPtr messagePtr)
    {
        var tcs = new TaskCompletionSource<TResult>();

        var regWaitHandle = ThreadPool.RegisterWaitForSingleObject(
            new WaitHandleSafe(hEvent),
            (cbState, _) =>
            {
                var message = Marshal.PtrToStructure<TResult>((IntPtr)cbState!);
                tcs.SetResult(message!);
            },
            messagePtr,
            -1,
            true
        );

        var result = await tcs.Task;
        regWaitHandle.Unregister(null);
        return result;
    }
}