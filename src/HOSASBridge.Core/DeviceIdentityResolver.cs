namespace HOSASBridge.Core;

public sealed class DeviceIdentityResolver : IDeviceIdentityResolver
{
    public DeviceIdentity? Resolve(DeviceIdentity stored, IReadOnlyList<DeviceIdentity> candidates)
    {
        var compatible = candidates.Where(c => c.Backend == stored.Backend && c.VendorId == stored.VendorId && c.ProductId == stored.ProductId).ToArray();
        var exact = compatible.Where(c => c.InstanceGuid == stored.InstanceGuid &&
            (string.IsNullOrEmpty(stored.InstanceId) || c.InstanceId.Equals(stored.InstanceId, StringComparison.OrdinalIgnoreCase))).ToArray();
        if (exact.Length == 1) return exact[0];
        if (stored.Backend == "XInput") return null;
        if (stored.ContainerId != Guid.Empty)
        {
            var sameContainer = compatible.Where(c => c.ContainerId == stored.ContainerId).ToArray();
            if (sameContainer.Length == 1) return sameContainer[0];
        }
        // A unique VID/PID or a newly occupied USB port does not prove physical identity.
        return null;
    }
}
