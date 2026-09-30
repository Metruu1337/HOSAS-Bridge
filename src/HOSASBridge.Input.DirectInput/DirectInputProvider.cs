using HOSASBridge.Core;
using Vortice.DirectInput;
using SharpGen.Runtime;

namespace HOSASBridge.Input.DirectInput;

public sealed class DirectInputProvider : IPhysicalInputProvider
{
    private readonly IDirectInput8 input = DInput.DirectInput8Create();
    private readonly Dictionary<Guid, Handle> handles = [];
    private readonly Dictionary<Guid, long> retryAt = [];
    private readonly IBridgeLog log;
    private readonly nint window;
    private IReadOnlyList<DeviceIdentity> devices = [];
    private Task<IReadOnlyList<DeviceIdentity>>? discovery;
    private sealed class Handle(IDirectInputDevice8 device) : IDisposable
    {
        public IDirectInputDevice8 Device { get; } = device;
        public JoystickState State = new();
        public void Dispose() { _ = Device.Unacquire(); Device.Dispose(); }
    }
    public DirectInputProvider(nint window, IBridgeLog log) { this.window = window; this.log = log; }
    public IReadOnlyList<DeviceIdentity> Discover() => devices;
    public void Refresh()
    {
        if (discovery is { IsCompleted: true })
        {
            var discovered = discovery.GetAwaiter().GetResult(); discovery = null;
            foreach (var device in discovered.Where(d => devices.All(old => old.InstanceGuid != d.InstanceGuid)))
                log.Write("Information", "input.discovered", $"{device.ProductName}; VID={device.VendorId:X4}; PID={device.ProductId:X4}; instance={device.InstanceId}; GUID={device.InstanceGuid}; container={device.ContainerId}; location={device.Location}");
            devices = discovered;
            foreach (var id in handles.Keys.Where(id => devices.All(d => d.InstanceGuid != id)).ToArray())
            { handles[id].Dispose(); handles.Remove(id); retryAt.Remove(id); log.Write("Information", "input.removed", id.ToString()); }
        }
        discovery ??= Task.Run(DiscoverDevices);
    }
    private IReadOnlyList<DeviceIdentity> DiscoverDevices()
    {
        using var enumerator = DInput.DirectInput8Create();
        var found = new List<DeviceIdentity>();
        foreach (var d in enumerator.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly))
        {
            try
            {
                using var device = enumerator.CreateDevice(d.InstanceGuid);
                var vid = device.Properties.VendorId;
                if (vid == 0x1234 && device.Properties.ProductId == 0xBEAD) continue;
                var path = device.Properties.InterfacePath;
                // Xbox controllers are read through XInput so LT and RT remain independent.
                if (path.Contains("IG_", StringComparison.OrdinalIgnoreCase)) continue;
                var metadata = WindowsDeviceMetadata.Read(path);
                var caps = device.Capabilities;
                found.Add(new(d.InstanceGuid, metadata.InstanceId, path, metadata.ContainerId, metadata.Location, d.ProductName, vid, device.Properties.ProductId)
                { Manufacturer = metadata.Manufacturer, AxisCount = caps.AxeCount, ButtonCount = caps.ButtonCount, PovCount = caps.PovCount, AvailableAxes = string.Join(", ", device.GetObjects(DeviceObjectTypeFlags.Axis).Select(o => o.Name)) });
            }
            catch (SharpGenException ex) { log.Write("Warning", "input.discovery", d.ProductName, ex); }
        }
        return found;
    }
    public bool Read(DeviceIdentity identity, PhysicalState destination)
    {
        try
        {
            if (!handles.TryGetValue(identity.InstanceGuid, out var handle))
            {
                if (retryAt.TryGetValue(identity.InstanceGuid, out var when) && Environment.TickCount64 < when) { destination.Clear(); return false; }
                var device = input.CreateDevice(identity.InstanceGuid);
                try
                {
                    device.SetDataFormat<RawJoystickState>().CheckError();
                    device.SetCooperativeLevel(window, CooperativeLevel.Background | CooperativeLevel.NonExclusive).CheckError();
                    device.Properties.Range = new InputRange(0, 65535);
                    device.Acquire().CheckError();
                    handle = new Handle(device); handles.Add(identity.InstanceGuid, handle);
                    log.Write("Information", "input.acquired", identity.InstanceId);
                }
                catch { device.Dispose(); throw; }
            }
            handle.Device.Poll().CheckError();
            handle.Device.GetCurrentJoystickState(ref handle.State);
            var state = handle.State;
            destination.Connected = true;
            destination.Axes[0] = state.X; destination.Axes[1] = state.Y;
            destination.Axes[2] = state.RotationZ; destination.Axes[3] = state.Sliders[0];
            destination.Axes[4] = state.Z; destination.Axes[5] = state.RotationX;
            destination.Axes[6] = state.RotationY; destination.Axes[7] = state.Sliders[1];
            state.Buttons.CopyTo(destination.Buttons, 0); state.PointOfViewControllers.CopyTo(destination.Povs, 0);
            return true;
        }
        catch (SharpGenException ex)
        {
            destination.Clear();
            if (handles.Remove(identity.InstanceGuid, out var lost)) lost.Dispose();
            retryAt[identity.InstanceGuid] = Environment.TickCount64 + 1000;
            log.Write("Warning", "input.unavailable", identity.InstanceId, ex); return false;
        }
    }
    public void Dispose() { foreach (var handle in handles.Values) handle.Dispose(); handles.Clear(); input.Dispose(); }
}
