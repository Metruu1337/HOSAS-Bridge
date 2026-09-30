using System.Runtime.InteropServices;
using HOSASBridge.Core;

namespace HOSASBridge.Input.DirectInput;

[StructLayout(LayoutKind.Sequential)]
public struct XInputGamepad
{
    public ushort Buttons;
    public byte LeftTrigger, RightTrigger;
    public short LeftX, LeftY, RightX, RightY;
}
[StructLayout(LayoutKind.Sequential)]
public struct XInputState { public uint Packet; public XInputGamepad Gamepad; }
public interface IXInputApi { bool Read(uint slot, out XInputState state); }
public sealed class WindowsXInputApi : IXInputApi
{
    [DllImport("xinput1_4.dll", ExactSpelling = true)]
    private static extern uint XInputGetState(uint index, out XInputState state);
    public bool Read(uint slot, out XInputState state) => XInputGetState(slot, out state) == 0;
}

// XInput exposes player slots, not physical identities. A lost connection invalidates
// its assignment; do not silently route a different controller occupying that slot.
public sealed class XInputProvider(IXInputApi api) : IPhysicalInputProvider
{
    private readonly DeviceIdentity?[] connected = new DeviceIdentity?[4];
    public IReadOnlyList<DeviceIdentity> Discover() => connected.OfType<DeviceIdentity>().ToArray();
    public void Refresh()
    {
        for (uint slot = 0; slot < connected.Length; slot++)
        {
            if (!api.Read(slot, out _)) { connected[slot] = null; continue; }
            connected[slot] ??= new(Guid.NewGuid(), $"XINPUT\\{slot}", "", Guid.Empty,
                $"Player {slot + 1}", $"Xbox / XInput · Player {slot + 1}", 0, 0)
            { Backend = "XInput", AxisCount = 6, ButtonCount = 10, PovCount = 1,
                AvailableAxes = "Left X, Left Y, Right X, Right Y, LT, RT" };
        }
    }
    public bool Read(DeviceIdentity identity, PhysicalState destination)
    {
        destination.Clear();
        for (uint slot = 0; slot < connected.Length; slot++)
        {
            if (connected[slot]?.InstanceGuid != identity.InstanceGuid) continue;
            if (!api.Read(slot, out var state)) { connected[slot] = null; return false; }
            Decode(state.Gamepad, destination); return true;
        }
        return false;
    }
    public static void Decode(XInputGamepad source, PhysicalState destination)
    {
        destination.Clear(); destination.Connected = true;
        static double Stick(short value) => 32767.5 + value * (32767.5 / (value < 0 ? 32768 : 32767));
        destination.Axes[(int)PhysicalAxis.X] = Stick(source.LeftX);
        destination.Axes[(int)PhysicalAxis.Y] = 65535 - Stick(source.LeftY);
        destination.Axes[(int)PhysicalAxis.Z] = Stick(source.RightX);
        destination.Axes[(int)PhysicalAxis.Twist] = 65535 - Stick(source.RightY);
        destination.Axes[(int)PhysicalAxis.Rx] = source.LeftTrigger * 257d;
        destination.Axes[(int)PhysicalAxis.Ry] = source.RightTrigger * 257d;
        ReadOnlySpan<ushort> masks = [0x1000, 0x2000, 0x4000, 0x8000, 0x0100, 0x0200, 0x0020, 0x0010, 0x0040, 0x0080];
        for (var i = 0; i < masks.Length; i++) destination.Buttons[i] = (source.Buttons & masks[i]) != 0;
        var x = ((source.Buttons & 8) != 0 ? 1 : 0) - ((source.Buttons & 4) != 0 ? 1 : 0);
        var y = ((source.Buttons & 2) != 0 ? 1 : 0) - ((source.Buttons & 1) != 0 ? 1 : 0);
        destination.Povs[0] = (x, y) switch
        { (0, -1) => 0, (1, -1) => 4500, (1, 0) => 9000, (1, 1) => 13500,
          (0, 1) => 18000, (-1, 1) => 22500, (-1, 0) => 27000, (-1, -1) => 31500, _ => -1 };
    }
    public void Dispose() => Array.Clear(connected);
}

public sealed class ControllerInputProvider(IPhysicalInputProvider directInput, IPhysicalInputProvider xinput) : IPhysicalInputProvider
{
    public IReadOnlyList<DeviceIdentity> Discover() => directInput.Discover().Concat(xinput.Discover()).ToArray();
    public void Refresh() { directInput.Refresh(); xinput.Refresh(); }
    public bool Read(DeviceIdentity identity, PhysicalState destination)
        => (identity.Backend == "XInput" ? xinput : directInput).Read(identity, destination);
    public void Dispose() { directInput.Dispose(); xinput.Dispose(); }
}
