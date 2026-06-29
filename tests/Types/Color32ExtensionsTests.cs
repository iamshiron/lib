using Shiron.Lib.Types;
using Shiron.Lib.Types.Ext.Conversion;
using Xunit;

namespace Shiron.Lib.Tests.Types;

public class Color32ExtensionsTests {
    [Fact]
    public void ToLabColor_Black_ReturnsLabBlack() {
        var color = new Color32(0, 0, 0, 255);

        var lab = color.ToLabColor();

        Assert.Equal(0.0, lab.L, 2);
        Assert.Equal(0.0, lab.A, 2);
        Assert.Equal(0.0, lab.B, 2);
    }

    [Fact]
    public void ToLabColor_White_ReturnsLabWhite() {
        var color = new Color32(255, 255, 255, 255);

        var lab = color.ToLabColor();

        Assert.Equal(100.0, lab.L, 2);
        Assert.Equal(0.0, lab.A, 2);
        Assert.Equal(0.0, lab.B, 2);
    }

    [Fact]
    public void ToLabColor_PureRed_HasPositiveA() {
        var color = new Color32(255, 0, 0, 255);

        var lab = color.ToLabColor();

        Assert.True(lab.L > 0);
        Assert.True(lab.A > 0);
    }

    [Fact]
    public void ToLabColor_PureGreen_HasNegativeA() {
        var color = new Color32(0, 255, 0, 255);

        var lab = color.ToLabColor();

        Assert.True(lab.L > 0);
        Assert.True(lab.A < 0);
    }

    [Fact]
    public void ToLabColor_PureBlue_HasNegativeB() {
        var color = new Color32(0, 0, 255, 255);

        var lab = color.ToLabColor();

        Assert.True(lab.L > 0);
        Assert.True(lab.B < 0);
    }

    [Fact]
    public void ToLabColor_MidGray_HasNearZeroChroma() {
        var color = new Color32(128, 128, 128, 255);

        var lab = color.ToLabColor();

        Assert.InRange(lab.A, -0.5, 0.5);
        Assert.InRange(lab.B, -0.5, 0.5);
        Assert.True(lab.L > 0);
        Assert.True(lab.L < 100);
    }

    [Fact]
    public void ToLabColor_IgnoresAlpha() {
        var opaque = new Color32(100, 150, 200, 255);
        var transparent = new Color32(100, 150, 200, 0);

        var labOpaque = opaque.ToLabColor();
        var labTransparent = transparent.ToLabColor();

        Assert.Equal(labTransparent.L, labOpaque.L, 10);
        Assert.Equal(labTransparent.A, labOpaque.A, 10);
        Assert.Equal(labTransparent.B, labOpaque.B, 10);
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

        Assert.InRange(lab.L, 0, 100);
    }
}
