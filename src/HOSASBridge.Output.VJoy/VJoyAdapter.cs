using HOSASBridge.Core;

namespace HOSASBridge.Output.VJoy;

public sealed class VJoyAdapter : IVirtualJoystick
{
    private readonly int[] minimum = new int[8], maximum = new int[8];
    private readonly VirtualState neutral = new();
    private uint owned;
    public Health Check(VirtualRequirements requirements)
    {
        try
        {
            if (!VJoyNative.vJoyEnabled()) return new(false, "vJoy driver is missing or disabled.");
            if (!VJoyNative.DriverMatch(out var dll, out var driver) || dll != 0x222 || driver != 0x222)
                return new(false, $"Unsupported vJoy API: DLL {dll:X}, driver {driver:X}; expected 222.");
            var status = VJoyNative.GetVJDStatus(requirements.DeviceId);
            if (status == 2) return new(false, $"vJoy {requirements.DeviceId} is busy (owner PID {VJoyNative.GetOwnerPid(requirements.DeviceId)}).");
            if (status > 2) return new(false, $"vJoy Device {requirements.DeviceId} is missing.");
            for (uint i = 0; i < 8; i++)
                if (!VJoyNative.GetVJDAxisExist(requirements.DeviceId, 0x30 + i)) return new(false, $"vJoy axis {(VirtualAxis)i} is missing.");
            if (VJoyNative.GetVJDButtonNumber(requirements.DeviceId) < requirements.Buttons || VJoyNative.GetVJDContPovNumber(requirements.DeviceId) < requirements.Povs)
                return new(false, "vJoy needs more buttons or continuous POVs. Run virtual device setup.");
            return new(true, $"vJoy {requirements.DeviceId} ready · API 2.2.2 · persistent device");
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        { return new(false, "vJoy interface unavailable: " + ex.Message); }
    }
    public bool Acquire(VirtualRequirements requirements)
    {
        if (owned == requirements.DeviceId) return true;
        if (!Check(requirements).Ready || !VJoyNative.AcquireVJD(requirements.DeviceId)) return false;
        owned = requirements.DeviceId;
        for (uint i = 0; i < 8; i++)
            if (!VJoyNative.GetVJDAxisMin(owned, 0x30 + i, out minimum[i]) || !VJoyNative.GetVJDAxisMax(owned, 0x30 + i, out maximum[i]))
            { Release(); return false; }
        Neutralize(); return true;
    }
    public static int Scale(double value, int min, int max) => (int)Math.Round(min + (Math.Clamp(value, -1, 1) + 1) * 0.5 * (max - min));
    public void Write(VirtualState state)
    {
        if (owned == 0) throw new InvalidOperationException("vJoy is not acquired.");
        int Axis(int index) => Scale(state.Axes[index], minimum[index], maximum[index]);
        uint Buttons(int start) { uint bits = 0; for (var i = 0; i < 32; i++) if (state.Buttons[start + i]) bits |= 1u << i; return bits; }
        var position = new VJoyNative.Position
        {
            Device = (byte)owned, X = Axis(0), Y = Axis(1), Z = Axis(2), Rx = Axis(3), Ry = Axis(4), Rz = Axis(5), Slider = Axis(6), Dial = Axis(7),
            Hat0 = unchecked((uint)state.Povs[0]), Hat1 = unchecked((uint)state.Povs[1]), Hat2 = unchecked((uint)state.Povs[2]), Hat3 = unchecked((uint)state.Povs[3]),
            Buttons = Buttons(0), Buttons1 = Buttons(32), Buttons2 = Buttons(64), Buttons3 = Buttons(96)
        };
        if (!VJoyNative.UpdateVJD(owned, ref position)) throw new IOException("vJoy rejected the output report.");
    }
    public void Neutralize() { if (owned != 0) Write(neutral); }
    public void Release()
    {
        if (owned == 0) return;
        try { Neutralize(); }
        finally { VJoyNative.RelinquishVJD(owned); owned = 0; }
    }
    public void Dispose() => Release();
}
