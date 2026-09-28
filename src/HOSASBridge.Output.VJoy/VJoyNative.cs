using System.Runtime.InteropServices;

namespace HOSASBridge.Output.VJoy;

internal static class VJoyNative
{
    private const string Library = "vJoyInterface.dll";
    // Exact v2.2.2 SDK JOYSTICK_POSITION_V3: Windows LONG is 32 bit even on x64.
    [StructLayout(LayoutKind.Sequential)]
    internal struct Position
    {
        public byte Device;
        public int Throttle, Rudder, Aileron, X, Y, Z, Rx, Ry, Rz, Slider, Dial;
        public int Wheel, Accelerator, Brake, Clutch, Steering, Vx, Vy;
        public uint Buttons, Hat0, Hat1, Hat2, Hat3, Buttons1, Buttons2, Buttons3;
        public int Vz, Vbrx, Vbry, Vbrz;
    }
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool vJoyEnabled();
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool DriverMatch(out ushort dll, out ushort driver);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int GetVJDStatus(uint id);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int GetOwnerPid(uint id);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int GetVJDButtonNumber(uint id);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int GetVJDContPovNumber(uint id);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GetVJDAxisExist(uint id, uint axis);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GetVJDAxisMin(uint id, uint axis, out int min);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GetVJDAxisMax(uint id, uint axis, out int max);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool AcquireVJD(uint id);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern void RelinquishVJD(uint id);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool UpdateVJD(uint id, ref Position position);
}
