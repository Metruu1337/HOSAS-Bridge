using HOSASBridge.Core;
namespace HOSASBridge.Core.Tests;
public sealed class IdentityTests
{
    private static DeviceIdentity Device(Guid? instance = null, Guid? container = null) => new(instance ?? Guid.NewGuid(), "HID\\test", "path", container ?? Guid.Empty, "USB", "T16000M", 0x44f, 0xb10a);
    [Fact] public void ExactIdentityResolves() { var d = Device(); Assert.Equal(d, new DeviceIdentityResolver().Resolve(d, [Device(), d])); }
    [Fact] public void IdenticalProductsDoNotGuess() { var d = Device(); Assert.Null(new DeviceIdentityResolver().Resolve(d, [Device(), Device()])); }
    [Fact] public void SingleProductStillDoesNotGuess() => Assert.Null(new DeviceIdentityResolver().Resolve(Device(), [Device()]));
    [Fact] public void StableUniqueContainerCanRecover() { var old = Device(container: Guid.NewGuid()); var moved = Device(container: old.ContainerId); Assert.Equal(moved, new DeviceIdentityResolver().Resolve(old, [Device(), moved])); }
    [Fact] public void AmbiguousContainerRejected() { var old = Device(container: Guid.NewGuid()); Assert.Null(new DeviceIdentityResolver().Resolve(old, [Device(container: old.ContainerId), Device(container: old.ContainerId)])); }
}
