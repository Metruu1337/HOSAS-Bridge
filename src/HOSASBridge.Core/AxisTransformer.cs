namespace HOSASBridge.Core;

public sealed class AxisTransformer : IAxisTransformer
{
    public static double Normalize(double value, double min, double center, double max)
    {
        if (!double.IsFinite(value) || !(min < center && center < max)) return 0;
        return Math.Clamp(value < center ? (value - center) / (center - min) : (value - center) / (max - center), -1, 1);
    }
    public double Transform(double raw, AxisTransform settings)
    {
        var c = settings.Calibration;
        var value = Normalize(raw, c.Min, c.Center, c.Max);
        var magnitude = Math.Abs(value);
        magnitude = Math.Clamp((magnitude - settings.CenterDeadzone) / (1 - settings.CenterDeadzone - settings.OuterDeadzone), 0, 1);
        if (settings.Curve == CurveKind.Exponential) magnitude = Math.Pow(magnitude, settings.Exponent);
        value = Math.CopySign(magnitude * settings.Sensitivity, value);
        value = Math.Clamp(value, -settings.Saturation, settings.Saturation);
        return settings.Invert ? -value : value;
    }
}
