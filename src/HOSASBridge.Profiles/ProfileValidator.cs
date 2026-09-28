using HOSASBridge.Core;

namespace HOSASBridge.Profiles;

public static class ProfileValidator
{
    public static IReadOnlyList<string> Validate(Profile p)
    {
        var errors = new List<string>();
        if (p.SchemaVersion != 2) errors.Add("Unsupported schema version.");
        if (string.IsNullOrWhiteSpace(p.ProfileId) || string.IsNullOrWhiteSpace(p.DisplayName)) errors.Add("Profile name and ID are required.");
        if (!Enum.IsDefined(p.Routing)) errors.Add("Unknown routing strategy.");
        if (p.Axes is null || p.Buttons is null || p.Povs is null || p.Modes is null || p.Devices is null || p.VirtualDevice is null || p.Hiding is null)
        { errors.Add("Required profile collections are missing."); return errors; }
        if (p.VirtualDevice.DeviceId is < 1 or > 16 || p.VirtualDevice.Buttons is < 32 or > 128 || p.VirtualDevice.Povs is < 2 or > 4)
            errors.Add("Virtual device requires ID 1–16, 32–128 buttons and 2–4 continuous POVs.");
        if (p.RoleRoutes is null) { errors.Add("Routing policies are missing."); return errors; }
        if (p.RoleRoutes.Any(r => !Enum.IsDefined(r.Key) || r.Value is null || (r.Value.Native && r.Value.Hidden))) { errors.Add("Invalid role routing policy."); return errors; }
        bool Routed(DeviceRole role) => p.Route(role).Virtualized;
        foreach (var a in p.Axes)
        {
            if (a is null || a.Transform is null || a.Transform.Calibration is null) { errors.Add("Axis transform is missing."); continue; }
            if (!Enum.IsDefined(a.Role) || !Enum.IsDefined(a.Source) || !Enum.IsDefined(a.Target)) errors.Add("Unknown axis or role.");
            var t = a.Transform; var c = t.Calibration;
            if (new[] { c.Min, c.Center, c.Max, t.CenterDeadzone, t.OuterDeadzone, t.Sensitivity, t.Exponent, t.Saturation }.Any(v => !double.IsFinite(v)))
                errors.Add("Axis values must be finite.");
            if (!(c.Min < c.Center && c.Center < c.Max)) errors.Add("Calibration must satisfy min < center < max.");
            if (t.CenterDeadzone < 0 || t.OuterDeadzone < 0 || t.CenterDeadzone + t.OuterDeadzone >= 1) errors.Add("Deadzones must be nonnegative and total less than 1.");
            if (t.Sensitivity is <= 0 or > 10 || t.Exponent is < 0.1 or > 10 || t.Saturation is <= 0 or > 1 || !Enum.IsDefined(t.Curve)) errors.Add("Invalid curve, sensitivity or saturation.");
        }
        if (p.Axes.Where(a => a is not null && Routed(a.Role)).GroupBy(a => a.Target).Any(g => g.Count() > 1)) errors.Add("Two active axes cannot share an output.");
        if (p.Buttons.Any(b => b is null || !Enum.IsDefined(b.Role) || b.Source is < 1 or > 128 || b.Target < 1 || b.Target > p.VirtualDevice.Buttons)) errors.Add("Invalid button mapping.");
        if (p.Povs.Any(h => h is null || !Enum.IsDefined(h.Role) || h.Source is < 1 or > 4 || h.Target < 1 || h.Target > p.VirtualDevice.Povs)) errors.Add("Invalid POV mapping.");
        if (p.Povs.Where(h => h is not null && Routed(h.Role)).GroupBy(h => h.Target).Any(g => g.Count() > 1)) errors.Add("Two active hats cannot share an output.");
        if (p.Modes.Length > 64) { errors.Add("Too many modes."); return errors; }
        if (p.Modes.Length == 0 || p.Modes.Any(m => m is null || string.IsNullOrWhiteSpace(m.Id) || m.InvertAxes is null))
        { errors.Add("At least one valid mode is required."); return errors; }
        if (p.Modes.Select(m => m.Id).Distinct().Count() != p.Modes.Length) errors.Add("Duplicate mode names.");
        var ids = p.Modes.Select(m => m.Id).ToHashSet();
        foreach (var mode in p.Modes)
        {
            if (mode.AxisOverrides is null) { errors.Add("Mode overrides are missing."); continue; }
            if (mode.AxisOverrides.Count > 0 || mode.ButtonOverrides is not null)
            {
                var simple = p with { Modes = [new("NORMAL", null, [])], ModeBinding = null,
                    Axes = mode.AxisOverrides.Select(a => new AxisMapping(DeviceRole.Right, PhysicalAxis.X, a.Key, a.Value)).ToArray(),
                    Buttons = mode.ButtonOverrides ?? [] };
                errors.AddRange(Validate(simple));
            }
        }
        if (p.Modes.Any(m => (m.Parent is not null && !ids.Contains(m.Parent)) || m.InvertAxes.Any(a => !Enum.IsDefined(a)))) errors.Add("Invalid mode parent or axis.");
        if (errors.Count == 0)
        {
            try { _ = new ModeController(p); } catch (ArgumentException e) { errors.Add(e.Message); }
        }
        if (p.ModeBinding is { } b && (!Enum.IsDefined(b.Role) || !Enum.IsDefined(b.Behavior) || b.Button is < 1 or > 128 || !ids.Contains(b.BaseMode) || !ids.Contains(b.ActiveMode) || b.BaseMode == b.ActiveMode)) errors.Add("Invalid mode binding.");
        if (p.Devices.Any(d => !Enum.IsDefined(d.Key) || d.Value is null || d.Value.InstanceGuid == Guid.Empty)) errors.Add("Device identity is missing.");
        if (p.Devices.Values.Select(d => d?.InstanceGuid).Distinct().Count() != p.Devices.Count) errors.Add("LEFT and RIGHT must be different physical devices.");
        if (p.Routing == RoutingStrategy.Hybrid && p.Hiding.HideLeft) errors.Add("Hybrid routing must keep LEFT visible.");
        return errors;
    }
    public static void EnsureValid(Profile profile)
    {
        var errors = Validate(profile);
        if (errors.Count > 0) throw new InvalidDataException(string.Join(Environment.NewLine, errors));
    }
}
