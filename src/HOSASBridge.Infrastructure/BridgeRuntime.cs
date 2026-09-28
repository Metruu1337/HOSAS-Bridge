using System.Collections.Concurrent;
using System.Diagnostics;
using HOSASBridge.Core;

namespace HOSASBridge.Infrastructure;

public sealed class BridgeRuntime : IDisposable
{
    private readonly IPhysicalInputProvider input;
    private readonly IVirtualJoystick output;
    private readonly IDeviceIdentityResolver resolver;
    private readonly Dictionary<DeviceRole, DeviceIdentity> resolved = [];
    private readonly IBridgeLog log;
    private readonly ConcurrentQueue<Action> commands = new();
    private readonly CancellationTokenSource cancellation = new();
    private readonly Thread worker;
    private Profile profile;
    private IInputPipeline pipeline;
    private bool requested, acquired;
    private long nextAcquire;
    private long observeAllUntil;
    public void ObserveForIdentification() => commands.Enqueue(() => observeAllUntil = Environment.TickCount64 + 31000);
    public void StopIdentification() => commands.Enqueue(() => observeAllUntil = 0);
    private int retryDelay = 1000;
    private string status = "STOPPED";
    private PipelineSnapshot? snapshot;
    private IReadOnlyList<DeviceIdentity> devices = [];
    private IReadOnlyDictionary<Guid, PhysicalState> identificationStates = new Dictionary<Guid, PhysicalState>();
    private Health virtualHealth = new(false, "Checking vJoy…");
    public PipelineSnapshot? Snapshot => Volatile.Read(ref snapshot);
    public IReadOnlyList<DeviceIdentity> Devices => Volatile.Read(ref devices);
    public IReadOnlyDictionary<Guid, PhysicalState> IdentificationStates => Volatile.Read(ref identificationStates);
    public Health VirtualHealth => Volatile.Read(ref virtualHealth);
    public BridgeRuntime(IPhysicalInputProvider input, IVirtualJoystick output, Profile profile, IBridgeLog log)
    {
        this.input = input; this.output = output; this.profile = profile; this.log = log;
        resolver = new DeviceIdentityResolver(); pipeline = new InputPipeline(profile);
        worker = new Thread(Run) { Name = "HOSAS input", IsBackground = true };
        worker.Start();
    }
    public void Start() => commands.Enqueue(() => { requested = true; nextAcquire = 0; retryDelay = 1000; });
    public void Stop() => commands.Enqueue(() => { requested = false; Release(); status = "STOPPED"; });
    public Task StopAsync()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        commands.Enqueue(() => { requested = false; Release(); status = "STOPPED"; completion.SetResult(); });
        return completion.Task;
    }
    public void SelectMode(string mode) => commands.Enqueue(() => pipeline.SelectMode(mode));
    public void Activate(Profile next) => commands.Enqueue(() =>
    {
        Release();
        if (!profile.RoleRoutes.OrderBy(p => p.Key).SequenceEqual(next.RoleRoutes.OrderBy(p => p.Key)) || profile.Routing != next.Routing || profile.VirtualDevice != next.VirtualDevice || profile.Hiding != next.Hiding || !profile.Devices.OrderBy(p => p.Key).SequenceEqual(next.Devices.OrderBy(p => p.Key)))
            requested = false; // Visibility/identity changes require a fresh health-checked Start.
        profile = next; pipeline = new InputPipeline(next); ResolveRoles(); nextAcquire = 0;
        status = requested ? "Starting…" : "STOPPED"; log.Write("Information", "profile.activated", next.DisplayName);
    });
    private void ResolveRoles()
    {
        resolved.Clear();
        foreach (var (role, stored) in profile.Devices)
            if (resolver.Resolve(stored, Devices) is { } match) resolved[role] = match;
        foreach (var group in resolved.GroupBy(p => p.Value.InstanceGuid).Where(g => g.Count() > 1).ToArray())
            foreach (var pair in group) resolved.Remove(pair.Key);
    }
    public Task TestOutputAsync(CancellationToken token)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        commands.Enqueue(() =>
        {
            if (requested || acquired) { completion.SetException(new InvalidOperationException("Stop the bridge before output testing.")); return; }
            try
            {
                if (!output.Acquire(profile.VirtualDevice)) throw new IOException(output.Check(profile.VirtualDevice).Detail);
                acquired = true;
                token.ThrowIfCancellationRequested();
                output.Write(new VirtualState()); // Health test sends only neutral output, never generated gameplay input.
                completion.SetResult();
            }
            catch (Exception ex) { completion.SetException(ex); }
            finally { Release(); }
        });
        return completion.Task;
    }
    private void Release()
    {
        if (!acquired) return;
        try { output.Release(); }
        catch (Exception ex) { log.Write("Error", "output.release", "Best-effort neutralization failed.", ex); }
        finally { acquired = false; }
    }
    private void Run()
    {
        var right = new PhysicalState(); var left = new PhysicalState(); var state = new VirtualState();
        right.Clear(); left.Clear();
        var roleStates = Enum.GetValues<DeviceRole>().ToDictionary(r => r, _ => new PhysicalState());
        roleStates[DeviceRole.Right] = right; roleStates[DeviceRole.Left] = left;
        long discoverAt = 0, publishAt = 0; var intervalStart = Stopwatch.GetTimestamp();
        int reads = 0, writes = 0; double inputHz = 0, outputHz = 0, processingMs = 0;
        string lastMode = pipeline.Mode;
        var capture = new Dictionary<Guid, PhysicalState>();
        try
        {
            using var clock = new InputClock();
            while (!cancellation.IsCancellationRequested)
            {
                var begin = Stopwatch.GetTimestamp(); var now = Environment.TickCount64;
                try
                {
                    while (commands.TryDequeue(out var command)) command();
                    if (now >= discoverAt)
                    {
                        input.Refresh(); Volatile.Write(ref devices, input.Discover());
                        ResolveRoles();
                        Volatile.Write(ref virtualHealth, output.Check(profile.VirtualDevice));
                        discoverAt = now + 2000;
                        capture = Devices.ToDictionary(d => d.InstanceGuid, _ => new PhysicalState());
                    }
                    foreach (var buffer in roleStates.Values) buffer.Clear();
                    foreach (var device in Devices)
                    {
                        var buffer = capture[device.InstanceGuid];
                        if (now < observeAllUntil || resolved.Values.Any(d => d.InstanceGuid == device.InstanceGuid)) input.Read(device, buffer);
                        else buffer.Clear();
                    }
                    void ReadRole(DeviceRole role, PhysicalState target)
                    {
                        if (!resolved.TryGetValue(role, out var match)) return;
                        var source = capture[match.InstanceGuid]; target.Connected = source.Connected;
                        source.Axes.CopyTo(target.Axes, 0); source.Buttons.CopyTo(target.Buttons, 0); source.Povs.CopyTo(target.Povs, 0);
                    }
                    foreach (var (role, buffer) in roleStates) ReadRole(role, buffer);
                    reads++;
                    pipeline.Process(roleStates, state);
                    if (pipeline.Mode != lastMode) { lastMode = pipeline.Mode; log.Write("Information", "mode.changed", lastMode); }
                    if (requested && !acquired && now >= nextAcquire)
                    {
                        acquired = output.Acquire(profile.VirtualDevice);
                        status = acquired ? "ACTIVE" : output.Check(profile.VirtualDevice).Detail;
                        log.Write(acquired ? "Information" : "Warning", "output.acquire", status);
                        nextAcquire = now + retryDelay; retryDelay = Math.Min(10000, retryDelay * 2);
                    }
                    if (acquired) { output.Write(state); writes++; status = right.Connected ? "ACTIVE" : "Waiting for RIGHT · output neutralized"; }
                    processingMs = Stopwatch.GetElapsedTime(begin).TotalMilliseconds;
                    if (Stopwatch.GetElapsedTime(intervalStart).TotalSeconds >= 1)
                    {
                        var seconds = Stopwatch.GetElapsedTime(intervalStart).TotalSeconds;
                        inputHz = reads / seconds; outputHz = writes / seconds; reads = writes = 0; intervalStart = Stopwatch.GetTimestamp();
                    }
                    if (now >= publishAt)
                    {
                        var actual = acquired ? state.Clone() : new VirtualState();
                        Volatile.Write(ref snapshot, new(DateTimeOffset.Now, right.Clone(), left.Clone(), actual, pipeline.Mode, acquired, status, inputHz, outputHz, processingMs) { Roles = roleStates.ToDictionary(p => p.Key, p => p.Value.Clone()) });
                        Volatile.Write(ref identificationStates, capture.ToDictionary(p => p.Key, p => p.Value.Clone()));
                        publishAt = now + 50;
                    }
                }
                catch (Exception ex)
                {
                    Release(); state.Clear(); status = ex.Message; nextAcquire = now + 2000;
                    log.Write("Error", "bridge.iteration", status, ex);
                    Volatile.Write(ref snapshot, new(DateTimeOffset.Now, right.Clone(), left.Clone(), state.Clone(), pipeline.Mode, false, status, inputHz, 0, processingMs));
                    cancellation.Token.WaitHandle.WaitOne(250);
                }
                clock.Wait(cancellation.Token);
            }
        }
        finally { Release(); input.Dispose(); output.Dispose(); }
    }
    public void Dispose() { cancellation.Cancel(); worker.Join(); cancellation.Dispose(); }
}
