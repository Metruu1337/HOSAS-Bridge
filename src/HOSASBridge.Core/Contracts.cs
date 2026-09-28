namespace HOSASBridge.Core;

public interface IDeviceDiscoveryService { IReadOnlyList<DeviceIdentity> Discover(); }
public interface IPhysicalInputProvider : IDeviceDiscoveryService, IDisposable
{
    bool Read(DeviceIdentity identity, PhysicalState destination);
    void Refresh();
}
public interface IDeviceIdentityResolver { DeviceIdentity? Resolve(DeviceIdentity stored, IReadOnlyList<DeviceIdentity> candidates); }
public interface IVirtualJoystick : IDisposable
{
    Health Check(VirtualRequirements requirements);
    bool Acquire(VirtualRequirements requirements);
    void Write(VirtualState state);
    void Neutralize();
    void Release();
}
public interface IDeviceHidingService
{
    Task<HidingHealth> CheckAsync(Profile profile, string executable, CancellationToken token);
    Task RepairAsync(Profile profile, string executable, CancellationToken token);
}
public interface IProfileRepository
{
    Profile Load(string path);
    void Save(string path, Profile profile);
}
public interface IAxisTransformer { double Transform(double raw, AxisTransform settings); }
public interface IModeController
{
    string Current { get; }
    void Select(string id);
    void Update(bool pressed);
    bool Inverts(VirtualAxis axis);
    AxisTransform? TransformFor(VirtualAxis axis);
    ButtonMapping[]? ButtonsForMode();
}
public interface IInputPipeline
{
    string Mode { get; }
    void SelectMode(string id);
    void Process(PhysicalState right, PhysicalState left, VirtualState output);
    void Process(IReadOnlyDictionary<DeviceRole, PhysicalState> states, VirtualState output);
}
public interface IApplicationStartupService { bool Enabled { get; } void SetEnabled(bool enabled, string executable); }
public interface IBridgeLog { void Write(string level, string eventName, string message, Exception? exception = null); }
