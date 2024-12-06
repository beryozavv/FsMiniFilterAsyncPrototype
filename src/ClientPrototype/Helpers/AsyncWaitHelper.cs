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
        await tcs.Task.ConfigureAwait(false);
        regWaitHandle.Unregister(null);
    }

    /// <summary>
    /// Подписка на событие и асинхронное ожидание коллбэка
    /// </summary>
    /// <param name="hEvent">Событие</param>
    /// <param name="messagePtr">Указатель на сообщение</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TResult">Тип сообщения</typeparam>
    /// <returns>Сообщение</returns>
    public static async Task<TResult> WaitForMessageAsync<TResult>(this IntPtr hEvent, IntPtr messagePtr,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<TResult>();

        var regWaitHandle = ThreadPool.RegisterWaitForSingleObject(
            new WaitHandleSafe(hEvent),
            static (cbState, _) =>
            {
                var (taskCompletionSource, state) = ((TaskCompletionSource<TResult>, IntPtr))cbState!;
                var message = Marshal.PtrToStructure<TResult>(state!);
                taskCompletionSource.TrySetResult(message!);
            },
            (tcs, messagePtr),
            -1,
            true
        );

        using (cancellationToken.Register(() => { tcs.TrySetCanceled(cancellationToken); }))
        {
            try
            {
                var result = await tcs.Task.ConfigureAwait(false);
                regWaitHandle.Unregister(null);
                return result;
            }
            catch (Exception)
            {
                regWaitHandle.Unregister(null);
                throw;
            }
        }
    }
}