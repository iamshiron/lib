using Shiron.Lib.Types;
using Shiron.Lib.Types.Ext.Conversion;
using Xunit;

namespace Shiron.Lib.Tests.Types;

public class LabColorExtensionsTests {
    [Fact]
    public void ToColor32_LabBlack_ReturnsBlack() {
        var lab = new LabColor(0.0, 0.0, 0.0);

        var color = lab.ToColor32();

        Assert.Equal(0, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
    }

    [Fact]
    public void ToColor32_LabWhite_ReturnsWhite() {
        var lab = new LabColor(100.0, 0.0, 0.0);

        var color = lab.ToColor32();

        Assert.Equal(255, color.R);
        Assert.Equal(255, color.G);
        Assert.Equal(255, color.B);
    }

    [Fact]
    public void ToColor32_DefaultAlpha_Is255() {
        var lab = new LabColor(50.0, 0.0, 0.0);

        var color = lab.ToColor32();

        Assert.Equal(255, color.A);
    }

    [Fact]
    public void ToColor32_WithCustomAlpha_SetsAlpha() {
        var lab = new LabColor(50.0, 0.0, 0.0);

        var color = lab.ToColor32(128);

        Assert.Equal(128, color.A);
    }

    [Fact]
    public void ToColor32_WithAlphaZero_IsTransparent() {
        var lab = new LabColor(50.0, 0.0, 0.0);

        var color = lab.ToColor32(0);

        Assert.Equal(0, color.A);
    }

    [Fact]
    public void ToColor32_RoundTrip_FromColor32_IsIdentity() {
        var original = new Color32(200, 100, 50, 255);

        var roundTripped = original.ToLabColor().ToColor32();

        Assert.Equal(original.R, roundTripped.R);
        Assert.Equal(original.G, roundTripped.G);
        Assert.Equal(original.B, roundTripped.B);
    }

    [Fact]
    public void ToColor32_RoundTrip_Black_IsIdentity() {
        var original = new Color32(0, 0, 0, 255);

        var roundTripped = original.ToLabColor().ToColor32();

        Assert.Equal(original.R, roundTripped.R);
        Assert.Equal(original.G, roundTripped.G);
        Assert.Equal(original.B, roundTripped.B);
    }

    [Fact]
    public void ToColor32_RoundTrip_White_IsIdentity() {
        var original = new Color32(255, 255, 255, 255);

        var roundTripped = original.ToLabColor().ToColor32();

        Assert.Equal(original.R, roundTripped.R);
        Assert.Equal(original.G, roundTripped.G);
        Assert.Equal(original.B, roundTripped.B);
    }

    [Theory]
    [InlineData(10, 20, 30)]
    [InlineData(200, 100, 50)]
    [InlineData(0, 128, 255)]
    [InlineData(255, 128, 0)]
    [InlineData(64, 192, 128)]
    public void ToColor32_RoundTrip_VariousColors_PreservesComponents(byte r, byte g, byte b) {
        var original = new Color32(r, g, b, 255);

        var roundTripped = original.ToLabColor().ToColor32();

        Assert.Equal(original.R, roundTripped.R);
        Assert.Equal(original.G, roundTripped.G);
        Assert.Equal(original.B, roundTripped.B);
    }

    [Fact]
    public void ToColor32_ClampsOutOfGamutLuminance() {
        var lab = new LabColor(150.0, 0.0, 0.0);

        var color = lab.ToColor32();

        Assert.Equal(255, color.R);
        Assert.Equal(255, color.G);
        Assert.Equal(255, color.B);
    }

    [Fact]
    public void ToColor32_ClampsNegativeLuminance() {
        var lab = new LabColor(-50.0, 0.0, 0.0);

        var color = lab.ToColor32();

        Assert.Equal(0, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
    }

    [Fact]
    public void ToColor32_MidGray_RoundTrips() {
        var original = new Color32(128, 128, 128, 255);

        var roundTripped = original.ToLabColor().ToColor32();

        Assert.Equal(original.R, roundTripped.R);
        Assert.Equal(original.G, roundTripped.G);
        Assert.Equal(original.B, roundTripped.B);
    }
}
