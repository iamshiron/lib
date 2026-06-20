using FluentAssertions;
using Shiron.Lib.Types;
using Xunit;

namespace Shiron.Lib.Tests.Types;

public class Color32Tests {
    [Fact]
    public void Constructor_WithRgbaBytes_SetsProperties() {
        var color = new Color32(10, 20, 30, 40);

        color.R.Should().Be(10);
        color.G.Should().Be(20);
        color.B.Should().Be(30);
        color.A.Should().Be(40);
    }

    [Fact]
    public void Constructor_WithMaxValues_SetsProperties() {
        var color = new Color32(255, 255, 255, 255);

        color.R.Should().Be(255);
        color.G.Should().Be(255);
        color.B.Should().Be(255);
        color.A.Should().Be(255);
    }

    [Fact]
    public void Constructor_WithZeroValues_SetsProperties() {
        var color = new Color32(0, 0, 0, 0);

        color.R.Should().Be(0);
        color.G.Should().Be(0);
        color.B.Should().Be(0);
        color.A.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithRgbaInt_ExtractsBytesCorrectly() {
        var color = new Color32(0x0A14_1E28);

        color.R.Should().Be(0x0A);
        color.G.Should().Be(0x14);
        color.B.Should().Be(0x1E);
        color.A.Should().Be(0x28);
    }

    [Fact]
    public void Constructor_WithRgbaInt_AllOnes_IsWhite() {
        var color = new Color32(unchecked((int) 0xFFFF_FFFF));

        color.R.Should().Be(255);
        color.G.Should().Be(255);
        color.B.Should().Be(255);
        color.A.Should().Be(255);
    }

    [Fact]
    public void Constructor_WithRgbaInt_AllZeros_IsBlackTransparent() {
        var color = new Color32(0x0000_0000);

        color.R.Should().Be(0);
        color.G.Should().Be(0);
        color.B.Should().Be(0);
        color.A.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithRgbIntAndAlpha_ExtractsBytesCorrectly() {
        var color = new Color32(0xFF_80_00, 0xCC);

        color.R.Should().Be(0xFF);
        color.G.Should().Be(0x80);
        color.B.Should().Be(0x00);
        color.A.Should().Be(0xCC);
    }

    [Fact]
    public void Constructor_WithRgbIntAndAlpha_RgbZeroAlphaNonZero() {
        var color = new Color32(0x000000, 128);

        color.R.Should().Be(0);
        color.G.Should().Be(0);
        color.B.Should().Be(0);
        color.A.Should().Be(128);
    }

    [Fact]
    public void Properties_AreMutable() {
        var color = new Color32(0, 0, 0, 0);

        color.R = 100;
        color.G = 150;
        color.B = 200;
        color.A = 250;

        color.R.Should().Be(100);
        color.G.Should().Be(150);
        color.B.Should().Be(200);
        color.A.Should().Be(250);
    }

    [Fact]
    public void ToRgba_PacksComponentsCorrectly() {
        var color = new Color32(0x12, 0x34, 0x56, 0x78);

        color.ToRgba.Should().Be(0x1234_5678);
    }

    [Fact]
    public void ToRgba_WithBlackOpaque_IsCorrect() {
        var color = new Color32(0, 0, 0, 255);

        color.ToRgba.Should().Be(0x0000_00FF);
    }

    [Fact]
    public void ToRgba_WithWhiteOpaque_IsCorrect() {
        var color = new Color32(255, 255, 255, 255);

        color.ToRgba.Should().Be(-1);
    }

    [Fact]
    public void ToRgb_PacksComponentsCorrectly() {
        var color = new Color32(0xAB, 0xCD, 0xEF, 0x12);

        color.ToRgb.Should().Be(0xAB_CD_EF);
    }

    [Fact]
    public void ToRgb_IgnoresAlpha() {
        var color1 = new Color32(10, 20, 30, 0);
        var color2 = new Color32(10, 20, 30, 255);

        color1.ToRgb.Should().Be(color2.ToRgb);
    }

    [Fact]
    public void ToHex_ReturnsFormattedHexString() {
        var color = new Color32(0x0A, 0x1B, 0x2C, 0x3D);

        color.ToHex().Should().Be("#0A1B2C3D");
    }

    [Fact]
    public void ToHex_WithWhiteOpaque_ReturnsFFFFFFFF() {
        var color = new Color32(255, 255, 255, 255);

        color.ToHex().Should().Be("#FFFFFFFF");
    }

    [Fact]
    public void ToHex_WithBlackOpaque_Returns000000FF() {
        var color = new Color32(0, 0, 0, 255);

        color.ToHex().Should().Be("#000000FF");
    }

    [Fact]
    public void ToHex_WithAllZero_Returns00000000() {
        var color = new Color32(0, 0, 0, 0);

        color.ToHex().Should().Be("#00000000");
    }

    [Fact]
    public void ToString_ReturnsSameAsToHex() {
        var color = new Color32(0x12, 0x34, 0x56, 0x78);

        color.ToString().Should().Be(color.ToHex());
    }

    [Fact]
    public void RoundTrip_RgbaIntConstructor_ToRgba_IsIdentity() {
        var original = 0x12_34_56_78;
        var color = new Color32(original);

        color.ToRgba.Should().Be(original);
    }

    [Fact]
    public void RoundTrip_ByteConstructor_ToRgba_IsIdentity() {
        var color = new Color32(100, 150, 200, 250);

        var reconstructed = new Color32(color.ToRgba);

        reconstructed.R.Should().Be(color.R);
        reconstructed.G.Should().Be(color.G);
        reconstructed.B.Should().Be(color.B);
        reconstructed.A.Should().Be(color.A);
    }

    [Theory]
    [InlineData(0xFF, 0x00, 0x00, 0xFF)]
    [InlineData(0x00, 0xFF, 0x00, 0xFF)]
    [InlineData(0x00, 0x00, 0xFF, 0xFF)]
    [InlineData(0x80, 0x80, 0x80, 0x80)]
    public void Constructor_WithBytes_MatchesRgbaRoundTrip(byte r, byte g, byte b, byte a) {
        var color = new Color32(r, g, b, a);

        var fromInt = new Color32(color.ToRgba);

        fromInt.R.Should().Be(r);
        fromInt.G.Should().Be(g);
        fromInt.B.Should().Be(b);
        fromInt.A.Should().Be(a);
    }
}
