using FluentAssertions;
using Shiron.Lib.Types;
using Shiron.Lib.Types.Ext.Conversion;
using Xunit;

namespace Shiron.Lib.Tests.Types;

public class Color32ExtensionsTests {
    [Fact]
    public void ToLabColor_Black_ReturnsLabBlack() {
        var color = new Color32(0, 0, 0, 255);

        var lab = color.ToLabColor();

        lab.L.Should().BeApproximately(0.0, 0.01);
        lab.A.Should().BeApproximately(0.0, 0.01);
        lab.B.Should().BeApproximately(0.0, 0.01);
    }

    [Fact]
    public void ToLabColor_White_ReturnsLabWhite() {
        var color = new Color32(255, 255, 255, 255);

        var lab = color.ToLabColor();

        lab.L.Should().BeApproximately(100.0, 0.01);
        lab.A.Should().BeApproximately(0.0, 0.01);
        lab.B.Should().BeApproximately(0.0, 0.01);
    }

    [Fact]
    public void ToLabColor_PureRed_HasPositiveA() {
        var color = new Color32(255, 0, 0, 255);

        var lab = color.ToLabColor();

        lab.L.Should().BeGreaterThan(0);
        lab.A.Should().BePositive();
    }

    [Fact]
    public void ToLabColor_PureGreen_HasNegativeA() {
        var color = new Color32(0, 255, 0, 255);

        var lab = color.ToLabColor();

        lab.L.Should().BeGreaterThan(0);
        lab.A.Should().BeNegative();
    }

    [Fact]
    public void ToLabColor_PureBlue_HasNegativeB() {
        var color = new Color32(0, 0, 255, 255);

        var lab = color.ToLabColor();

        lab.L.Should().BeGreaterThan(0);
        lab.B.Should().BeNegative();
    }

    [Fact]
    public void ToLabColor_MidGray_HasNearZeroChroma() {
        var color = new Color32(128, 128, 128, 255);

        var lab = color.ToLabColor();

        lab.A.Should().BeApproximately(0.0, 0.5);
        lab.B.Should().BeApproximately(0.0, 0.5);
        lab.L.Should().BeGreaterThan(0);
        lab.L.Should().BeLessThan(100);
    }

    [Fact]
    public void ToLabColor_IgnoresAlpha() {
        var opaque = new Color32(100, 150, 200, 255);
        var transparent = new Color32(100, 150, 200, 0);

        var labOpaque = opaque.ToLabColor();
        var labTransparent = transparent.ToLabColor();

        labOpaque.L.Should().BeApproximately(labTransparent.L, 1e-10);
        labOpaque.A.Should().BeApproximately(labTransparent.A, 1e-10);
        labOpaque.B.Should().BeApproximately(labTransparent.B, 1e-10);
    }

    [Theory]
    [InlineData(255, 0, 0)]
    [InlineData(0, 255, 0)]
    [InlineData(0, 0, 255)]
    [InlineData(128, 128, 128)]
    [InlineData(255, 255, 0)]
    [InlineData(0, 255, 255)]
    [InlineData(255, 0, 255)]
    public void ToLabColor_ProducesValidLRange(byte r, byte g, byte b) {
        var color = new Color32(r, g, b, 255);

        var lab = color.ToLabColor();

        lab.L.Should().BeInRange(0, 100);
    }
}
