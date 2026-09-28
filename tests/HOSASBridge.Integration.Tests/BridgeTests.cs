using HOSASBridge.Core;
using HOSASBridge.Infrastructure;
using HOSASBridge.Profiles;

namespace HOSASBridge.Integration.Tests;

public sealed class BridgeTests
{
    private static readonly DeviceIdentity Right = new(Guid.NewGuid(), "HID\\RIGHT", "right", Guid.NewGuid(), "USB1", "T16000M", 0x44f, 0xb10a);
    private static readonly DeviceIdentity Left = new(Guid.NewGuid(), "HID\\LEFT", "left", Guid.NewGuid(), "USB2", "T16000M", 0x44f, 0xb10a);
    private static Profile Profile()
    {
        var p = ProfileDefaults.Wardogs();
        return p with { Devices = new() { [DeviceRole.Right] = Right, [DeviceRole.Left] = Left }, Axes = p.Axes.Select(a => a with { Transform = new() { Calibration = new(-1, 0, 1), CenterDeadzone = 0 } }).ToArray() };
    }
    private static async Task Until(Func<bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!predicate()) await Task.Delay(10, timeout.Token);
    }
    [Fact] public async Task FullWorkerNormalMinigunAndHybrid()
    {
        var input = new FakeInput(); var output = new FakeVirtual(); using var bridge = new BridgeRuntime(input, output, Profile(), new TestLog());
        bridge.Start(); await Until(() => output.Last?.Axes[1] == 0.7); var normal = output.Last!;
        bridge.SelectMode("MINIGUN"); await Until(() => output.Last?.Axes[1] == -0.7);
        Assert.Equal(normal.Axes[0], output.Last!.Axes[0]); Assert.Equal(normal.Axes[2], output.Last.Axes[2]);
        Assert.Equal(0, output.Last.Axes[(int)VirtualAxis.Slider0]); Assert.False(output.Last.Buttons[16]);
    }
    [Fact] public async Task WorkerDisconnectNeutralizesAndReconnects()
    {
        var input = new FakeInput(); var output = new FakeVirtual(); using var bridge = new BridgeRuntime(input, output, Profile(), new TestLog());
        bridge.Start(); await Until(() => output.Last?.Buttons[0] == true);
        input.Connected = false; await Until(() => output.Last?.Buttons[0] == false);
        Assert.All(output.Last!.Axes, a => Assert.Equal(0, a)); Assert.All(output.Last.Povs, h => Assert.Equal(-1, h));
        input.Connected = true; await Until(() => output.Last?.Buttons[0] == true);
    }
    [Fact] public async Task StopReleasesOwnershipAndNeutralizes()
    {
        var output = new FakeVirtual(); using var bridge = new BridgeRuntime(new FakeInput(), output, Profile(), new TestLog());
        bridge.Start(); await Until(() => output.Last?.Buttons[0] == true); await bridge.StopAsync();
        Assert.False(output.Owned); Assert.All(output.Last!.Buttons, Assert.False); Assert.All(output.Last.Axes, a => Assert.Equal(0, a));
    }
    [Fact] public async Task UnavailableVirtualRetriesWithoutBusyLoop()
    {
        var output = new FakeVirtual { Available = false }; using var bridge = new BridgeRuntime(new FakeInput(), output, Profile(), new TestLog());
        bridge.Start(); await Until(() => output.Attempts > 0); await Task.Delay(200); Assert.Equal(1, output.Attempts);
        output.Available = true; await Until(() => output.Owned); Assert.True(output.Attempts <= 3);
    }
    [Fact] public async Task OutputFailureReleasesThenRecovers()
    {
        var output = new FakeVirtual(); using var bridge = new BridgeRuntime(new FakeInput(), output, Profile(), new TestLog());
        bridge.Start(); await Until(() => output.Owned); output.FailNext = true;
        await Until(() => output.Releases > 0); await Until(() => output.Owned); Assert.NotNull(output.Last);
    }
    [Fact] public async Task FakeHidingCanVerifyProfilePolicy()
    {
        IDeviceHidingService hiding = new FakeHiding(); var p = Profile(); await hiding.RepairAsync(p, "bridge.exe", CancellationToken.None);
        var health = await hiding.CheckAsync(p, "bridge.exe", CancellationToken.None); Assert.True(health.RightHidden); Assert.False(health.LeftHidden);
    }
    [Fact] public async Task RoutingChangeStopsUntilVisibilityIsCheckedAgain()
    {
        var output = new FakeVirtual(); var profile = Profile();
        using var bridge = new BridgeRuntime(new FakeInput(), output, profile, new TestLog());
        bridge.Start(); await Until(() => output.Owned);
        bridge.Activate(profile with { Routing = RoutingStrategy.CombinedVirtual, Hiding = new(true, true) });
        await Until(() => output.Releases > 0); await Task.Delay(100);
        Assert.False(output.Owned); Assert.All(output.Last!.Buttons, Assert.False);
    }
    private sealed class FakeInput : IPhysicalInputProvider
    {
        public volatile bool Connected = true;
        public IReadOnlyList<DeviceIdentity> Discover() => [Right, Left];
        public void Refresh() { }
        public bool Read(DeviceIdentity identity, PhysicalState destination)
        {
            destination.Clear(); if (!Connected) return false;
            destination.Connected = true; destination.Axes[0] = 0.2; destination.Axes[1] = 0.7; destination.Axes[2] = -0.3; destination.Axes[3] = 0;
            destination.Buttons[0] = true; destination.Povs[0] = 18000; return true;
        }
        public void Dispose() { }
    }
    private sealed class FakeVirtual : IVirtualJoystick
    {
        public volatile bool Available = true, Owned, FailNext;
        public volatile int Attempts, Releases;
        private VirtualState? last;
        public VirtualState? Last => Volatile.Read(ref last);
        public Health Check(VirtualRequirements requirements) => new(Available, "Fake test device");
        public bool Acquire(VirtualRequirements requirements) { Attempts++; Owned = Available; return Owned; }
        public void Write(VirtualState state) { if (FailNext) { FailNext = false; throw new IOException("Test disconnect"); } Volatile.Write(ref last, state.Clone()); }
        public void Neutralize() => Volatile.Write(ref last, new VirtualState());
        public void Release() { Neutralize(); Owned = false; Releases++; }
        public void Dispose() => Release();
    }
    private sealed class FakeHiding : IDeviceHidingService
    {
        private HidingPolicy policy = new(false, false);
        public Task<HidingHealth> CheckAsync(Profile p, string exe, CancellationToken token) => Task.FromResult(new HidingHealth(true, true, policy.HideRight, policy.HideLeft, true, "test"));
        public Task RepairAsync(Profile p, string exe, CancellationToken token) { policy = p.Hiding; return Task.CompletedTask; }
    }
    private sealed class TestLog : IBridgeLog { public void Write(string level, string eventName, string message, Exception? exception = null) { } }
}
