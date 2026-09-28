using System.Runtime.InteropServices;
using HOSASBridge.Output.VJoy;
namespace HOSASBridge.Core.Tests;
public sealed class NativeLayoutTests
{
    [Fact] public void ReportMatchesPinnedV3SdkLayout()
    {
        Assert.Equal(124, Marshal.SizeOf<VJoyNative.Position>());
        Assert.Equal(16, Marshal.OffsetOf<VJoyNative.Position>(nameof(VJoyNative.Position.X)).ToInt32());
        Assert.Equal(76, Marshal.OffsetOf<VJoyNative.Position>(nameof(VJoyNative.Position.Buttons)).ToInt32());
        Assert.Equal(80, Marshal.OffsetOf<VJoyNative.Position>(nameof(VJoyNative.Position.Hat0)).ToInt32());
        Assert.Equal(108, Marshal.OffsetOf<VJoyNative.Position>(nameof(VJoyNative.Position.Vz)).ToInt32());
    }
}
