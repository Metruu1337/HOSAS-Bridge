using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace HOSASBridge.Infrastructure;

internal sealed class InputClock : IDisposable
{
    private readonly SafeWaitHandle timer;
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern SafeWaitHandle CreateWaitableTimerEx(nint attributes, string? name, uint flags, uint access);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetWaitableTimer(SafeWaitHandle timer, ref long due, int period, nint callback, nint argument, bool resume);
    [DllImport("kernel32.dll")] private static extern uint WaitForSingleObject(SafeWaitHandle handle, uint milliseconds);
    public InputClock()
    {
        timer = CreateWaitableTimerEx(0, null, 2, 0x1F0003);
        if (timer.IsInvalid) { timer.Dispose(); timer = CreateWaitableTimerEx(0, null, 0, 0x1F0003); }
    }
    public void Wait(CancellationToken token)
    {
        if (token.IsCancellationRequested) return;
        long due = -20000; // Relative 100 ns units, 2 ms. High-resolution wait; no spin loop.
        if (!timer.IsInvalid && SetWaitableTimer(timer, ref due, 0, 0, 0, false)) _ = WaitForSingleObject(timer, 20);
        else token.WaitHandle.WaitOne(2);
    }
    public void Dispose() => timer.Dispose();
}
