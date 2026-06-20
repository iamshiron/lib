using Shiron.Lib.Types;
using Xunit;

namespace Shiron.Lib.Tests.Types;

public class LabColorTests {
    [Fact]
    public void Constructor_SetsProperties() {
        var lab = new LabColor(50.0, -20.0, 30.0);

        Assert.Equal(50.0, lab.L);
        Assert.Equal(-20.0, lab.A);
        Assert.Equal(30.0, lab.B);
    }

    [Fact]
    public void Constructor_WithZeroValues_SetsProperties() {
        var lab = new LabColor(0.0, 0.0, 0.0);

        Assert.Equal(0.0, lab.L);
        Assert.Equal(0.0, lab.A);
        Assert.Equal(0.0, lab.B);
    }

    [Fact]
    public void Properties_AreMutable() {
        var lab = new LabColor(0, 0, 0);

        lab.L = 75.5;
        lab.A = -10.3;
        lab.B = 42.1;

        Assert.Equal(75.5, lab.L);
        Assert.Equal(-10.3, lab.A);
        Assert.Equal(42.1, lab.B);
    }

    [Fact]
    public void DistanceTo_SamePoint_IsZero() {
        var lab = new LabColor(50.0, 25.0, -30.0);

        Assert.Equal(0.0, lab.DistanceTo(lab), 10);
    }

    [Fact]
    public void DistanceTo_TwoIdenticalColors_IsZero() {
        var lab1 = new LabColor(50.0, 25.0, -30.0);
        var lab2 = new LabColor(50.0, 25.0, -30.0);

        Assert.Equal(0.0, lab1.DistanceTo(lab2), 10);
    }

    [Fact]
    public void DistanceTo_IsSymmetric() {
        var lab1 = new LabColor(10.0, 20.0, 30.0);
        var lab2 = new LabColor(40.0, -10.0, 60.0);

        Assert.Equal(lab2.DistanceTo(lab1), lab1.DistanceTo(lab2), 10);
    }

    [Fact]
    public void DistanceTo_WithKnownValues_IsCorrect() {
        var lab1 = new LabColor(0.0, 0.0, 0.0);
        var lab2 = new LabColor(3.0, 4.0, 0.0);

        Assert.Equal(5.0, lab1.DistanceTo(lab2), 10);
    }

    [Fact]
    public void DistanceTo_WithOnlyLDifference_IsCorrect() {
        var lab1 = new LabColor(0.0, 0.0, 0.0);
        var lab2 = new LabColor(100.0, 0.0, 0.0);

        Assert.Equal(100.0, lab1.DistanceTo(lab2), 10);
    }

    [Fact]
    public void DistanceTo_WithAllAxesOffset_IsCorrect() {
        var lab1 = new LabColor(0.0, 0.0, 0.0);
        var lab2 = new LabColor(1.0, 1.0, 1.0);

        Assert.Equal(Math.Sqrt(3.0), lab1.DistanceTo(lab2), 10);
    }

    [Fact]
    public void ToString_ReturnsFormattedString() {
        var lab = new LabColor(50.123, -20.456, 30.789);

        Assert.Equal("Lab(50.12, -20.46, 30.79)", lab.ToString());
    }

    [Fact]
    public void ToString_WithZeroValues_ReturnsZeroedString() {
        var lab = new LabColor(0.0, 0.0, 0.0);

        Assert.Equal("Lab(0.00, 0.00, 0.00)", lab.ToString());
    }

    [Fact]
    public void ToString_IsOverridden() {
        var lab = new LabColor(50.0, 25.0, -30.0);
        var asObject = (object) lab;

        Assert.Equal(lab.ToString(), asObject.ToString());
    }
}
