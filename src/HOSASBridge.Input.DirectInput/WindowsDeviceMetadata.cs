using System.Runtime.InteropServices;
using System.Text;

namespace HOSASBridge.Input.DirectInput;

internal static class WindowsDeviceMetadata
{
    [StructLayout(LayoutKind.Sequential)]
    private struct DeviceInfo { public uint Size; public Guid ClassGuid; public uint DevInst; public nint Reserved; }
    [StructLayout(LayoutKind.Sequential)]
    private struct InterfaceInfo { public uint Size; public Guid ClassGuid; public uint Flags; public nint Reserved; }
    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey { public Guid Format; public uint Id; }
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode)] private static extern nint SetupDiCreateDeviceInfoList(nint guid, nint hwnd);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetupDiOpenDeviceInterface(nint set, string path, uint flags, ref InterfaceInfo data);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetupDiGetDeviceInterfaceDetail(nint set, ref InterfaceInfo data, nint detail, uint size, out uint needed, ref DeviceInfo info);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetupDiGetDeviceInstanceId(nint set, ref DeviceInfo info, StringBuilder text, uint size, out uint needed);
    [DllImport("setupapi.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetupDiDestroyDeviceInfoList(nint set);
    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)] private static extern uint CM_Get_DevNode_PropertyW(uint devInst, ref PropertyKey key, out uint type, byte[] buffer, ref uint length, uint flags);

    public static (string InstanceId, Guid ContainerId, string Location, string Manufacturer) Read(string path)
    {
        var set = SetupDiCreateDeviceInfoList(0, 0);
        if (set == -1) return ("", Guid.Empty, "", "");
        try
        {
            var iface = new InterfaceInfo { Size = (uint)Marshal.SizeOf<InterfaceInfo>() };
            if (!SetupDiOpenDeviceInterface(set, path, 0, ref iface)) return ("", Guid.Empty, "", "");
            var info = new DeviceInfo { Size = (uint)Marshal.SizeOf<DeviceInfo>() };
            _ = SetupDiGetDeviceInterfaceDetail(set, ref iface, 0, 0, out var size, ref info);
            if (size == 0) return ("", Guid.Empty, "", "");
            var detail = Marshal.AllocHGlobal((int)size);
            try
            {
                Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                if (!SetupDiGetDeviceInterfaceDetail(set, ref iface, detail, size, out _, ref info)) return ("", Guid.Empty, "", "");
            }
            finally { Marshal.FreeHGlobal(detail); }
            var id = new StringBuilder(512);
            if (!SetupDiGetDeviceInstanceId(set, ref info, id, 512, out _)) return ("", Guid.Empty, "", "");
            var key = new PropertyKey { Format = new("8c7ed206-3f8a-4827-b3ab-ae9e1faefc6c"), Id = 2 };
            var container = new byte[16]; uint length = 16;
            var guid = CM_Get_DevNode_PropertyW(info.DevInst, ref key, out _, container, ref length, 0) == 0 ? new Guid(container) : Guid.Empty;
            key = new PropertyKey { Format = new("a45c254e-df1c-4efd-8020-67d146a850e0"), Id = 15 };
            var location = new byte[2048]; length = (uint)location.Length;
            var text = CM_Get_DevNode_PropertyW(info.DevInst, ref key, out _, location, ref length, 0) == 0 ? Encoding.Unicode.GetString(location, 0, (int)length).TrimEnd('\0') : "";
            key = new PropertyKey { Format = new("a45c254e-df1c-4efd-8020-67d146a850e0"), Id = 13 };
            var maker = new byte[2048]; length = (uint)maker.Length;
            var manufacturer = CM_Get_DevNode_PropertyW(info.DevInst, ref key, out _, maker, ref length, 0) == 0 ? Encoding.Unicode.GetString(maker, 0, (int)length).TrimEnd('\0') : "";
            return (id.ToString(), guid, text, manufacturer);
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
    }
}
