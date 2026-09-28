using System.IO.Pipes;
using System.Runtime.Versioning;

namespace HOSASBridge.Infrastructure;

[SupportedOSPlatform("windows")]
public sealed class SingleInstance : IDisposable
{
    private readonly Mutex mutex;
    private readonly CancellationTokenSource cancellation = new();
    private readonly string pipeName;
    public bool IsFirst { get; }
    public SingleInstance(string? testScope = null)
    {
        var sid = System.Security.Principal.WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName;
        pipeName = "HOSASBridge-" + sid + (testScope is null ? "" : "-" + testScope);
        mutex = new Mutex(true, @"Local\" + pipeName, out var created); IsFirst = created;
    }
    public async Task NotifyAsync()
    {
        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
        await client.ConnectAsync(3000).ConfigureAwait(false);
        await client.WriteAsync(new byte[] { 1 }).ConfigureAwait(false);
    }
    public async Task ListenAsync(Action activate)
    {
        while (!cancellation.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(cancellation.Token).ConfigureAwait(false);
                var buffer = new byte[1]; await server.ReadAsync(buffer, cancellation.Token).ConfigureAwait(false); activate();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { break; }
        }
    }
    public void Dispose() { cancellation.Cancel(); if (IsFirst) mutex.ReleaseMutex(); mutex.Dispose(); }
}
