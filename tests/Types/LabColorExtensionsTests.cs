using FluentAssertions;
using Shiron.Lib.Types;
using Shiron.Lib.Types.Ext.Conversion;
using Xunit;

namespace Shiron.Lib.Tests.Types;

public class LabColorExtensionsTests {
    [Fact]
    public void ToColor32_LabBlack_ReturnsBlack() {
        var lab = new LabColor(0.0, 0.0, 0.0);

        var color = lab.ToColor32();

        color.R.Should().Be(0);
        color.G.Should().Be(0);
        color.B.Should().Be(0);
    }

    [Fact]
    public void ToColor32_LabWhite_ReturnsWhite() {
        var lab = new LabColor(100.0, 0.0, 0.0);

        var color = lab.ToColor32();

        color.R.Should().Be(255);
        color.G.Should().Be(255);
        color.B.Should().Be(255);
    }

    [Fact]
    public void ToColor32_DefaultAlpha_Is255() {
        var lab = new LabColor(50.0, 0.0, 0.0);

        var color = lab.ToColor32();

        color.A.Should().Be(255);
    }

    [Fact]
    public void ToColor32_WithCustomAlpha_SetsAlpha() {
        var lab = new LabColor(50.0, 0.0, 0.0);

        var color = lab.ToColor32(128);

        color.A.Should().Be(128);
    }

    [Fact]
    public void ToColor32_WithAlphaZero_IsTransparent() {
        var lab = new LabColor(50.0, 0.0, 0.0);

        var color = lab.ToColor32(0);

        color.A.Should().Be(0);
    }

    [Fact]
    public void ToColor32_RoundTrip_FromColor32_IsIdentity() {
        var original = new Color32(200, 100, 50, 255);

        var roundTripped = original.ToLabColor().ToColor32();

        roundTripped.R.Should().Be(original.R);
        roundTripped.G.Should().Be(original.G);
        roundTripped.B.Should().Be(original.B);
    }

    [Fact]
    public void ToColor32_RoundTrip_Black_IsIdentity() {
        var original = new Color32(0, 0, 0, 255);

        var roundTripped = original.ToLabColor().ToColor32();

        roundTripped.R.Should().Be(original.R);
        roundTripped.G.Should().Be(original.G);
        roundTripped.B.Should().Be(original.B);
    }

    [Fact]
    public void ToColor32_RoundTrip_White_IsIdentity() {
        var original = new Color32(255, 255, 255, 255);

        var roundTripped = original.ToLabColor().ToColor32();

        roundTripped.R.Should().Be(original.R);
        roundTripped.G.Should().Be(original.G);
        roundTripped.B.Should().Be(original.B);
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

        roundTripped.R.Should().Be(original.R);
        roundTripped.G.Should().Be(original.G);
        roundTripped.B.Should().Be(original.B);
    }

    [Fact]
    public void ToColor32_ClampsOutOfGamutLuminance() {
        var lab = new LabColor(150.0, 0.0, 0.0);

        var color = lab.ToColor32();

        color.R.Should().Be(255);
        color.G.Should().Be(255);
        color.B.Should().Be(255);
    }

    [Fact]
    public void ToColor32_ClampsNegativeLuminance() {
        var lab = new LabColor(-50.0, 0.0, 0.0);

        var color = lab.ToColor32();

        color.R.Should().Be(0);
        color.G.Should().Be(0);
        color.B.Should().Be(0);
    }

    [Fact]
    public void ToColor32_MidGray_RoundTrips() {
        var original = new Color32(128, 128, 128, 255);

        var roundTripped = original.ToLabColor().ToColor32();

        roundTripped.R.Should().Be(original.R);
        roundTripped.G.Should().Be(original.G);
        roundTripped.B.Should().Be(original.B);
    }
}
