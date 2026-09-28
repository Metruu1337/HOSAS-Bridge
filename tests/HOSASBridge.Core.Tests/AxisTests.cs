using HOSASBridge.Core;
using HOSASBridge.Output.VJoy;

namespace HOSASBridge.Core.Tests;

public sealed class AxisTests
{
    private readonly AxisTransformer transformer = new();
    private static AxisTransform Linear => new() { Calibration = new(-1, 0, 1), CenterDeadzone = 0 };
    [Theory]
    [InlineData(0, -1)][InlineData(32767.5, 0)][InlineData(65535, 1)][InlineData(-500, -1)][InlineData(70000, 1)]
    public void Normalization(double raw, double expected) => Assert.Equal(expected, AxisTransformer.Normalize(raw, 0, 32767.5, 65535), 10);
    [Theory]
    [InlineData(-1)][InlineData(-0.7)][InlineData(0)][InlineData(0.2)][InlineData(1)]
    public void LinearPreservesInput(double value) => Assert.Equal(value, transformer.Transform(value, Linear), 10);
    [Theory]
    [InlineData(0)][InlineData(0.04)][InlineData(-0.1)][InlineData(0.1)]
    public void CenterDeadzone(double value) => Assert.Equal(0, transformer.Transform(value, Linear with { CenterDeadzone = 0.1 }), 10);
    [Fact] public void DeadzoneRescalesRemainingTravel() => Assert.Equal(0.5, transformer.Transform(0.55, Linear with { CenterDeadzone = 0.1 }), 10);
    [Theory][InlineData(0.9, 1)][InlineData(1, 1)][InlineData(-0.95, -1)]
    public void OuterDeadzone(double value, double expected) => Assert.Equal(expected, transformer.Transform(value, Linear with { OuterDeadzone = 0.1 }), 10);
    [Fact] public void ExponentialCurve() => Assert.Equal(-0.25, transformer.Transform(-0.5, Linear with { Curve = CurveKind.Exponential, Exponent = 2 }), 10);
    [Fact] public void Inversion() => Assert.Equal(-0.7, transformer.Transform(0.7, Linear with { Invert = true }), 10);
    [Fact] public void Saturation() => Assert.Equal(0.6, transformer.Transform(0.9, Linear with { Saturation = 0.6 }), 10);
    [Fact] public void Sensitivity() => Assert.Equal(0.5, transformer.Transform(0.25, Linear with { Sensitivity = 2 }), 10);
    [Fact] public void AsymmetricCalibration() => Assert.Equal(-0.5, transformer.Transform(15, Linear with { Calibration = new(10, 20, 80) }), 10);
    [Fact] public void TransformOrder() => Assert.Equal(-0.5, transformer.Transform(0.55, Linear with { CenterDeadzone = 0.1, Curve = CurveKind.Exponential, Exponent = 2, Sensitivity = 2, Invert = true }), 10);
    [Fact] public void NonfiniteRawNeutralizes() => Assert.Equal(0, transformer.Transform(double.NaN, Linear));
    [Theory][InlineData(-1, 0)][InlineData(0, 16384)][InlineData(1, 32767)]
    public void NativeScaling(double value, int expected) => Assert.Equal(expected, VJoyAdapter.Scale(value, 0, 32767));
}
