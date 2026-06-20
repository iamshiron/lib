using FluentAssertions;
using Shiron.Lib.Types;
using Xunit;

namespace Shiron.Lib.Tests.Types;

public class LabColorTests {
    [Fact]
    public void Constructor_SetsProperties() {
        var lab = new LabColor(50.0, -20.0, 30.0);

        lab.L.Should().Be(50.0);
        lab.A.Should().Be(-20.0);
        lab.B.Should().Be(30.0);
    }

    [Fact]
    public void Constructor_WithZeroValues_SetsProperties() {
        var lab = new LabColor(0.0, 0.0, 0.0);

        lab.L.Should().Be(0.0);
        lab.A.Should().Be(0.0);
        lab.B.Should().Be(0.0);
    }

    [Fact]
    public void Properties_AreMutable() {
        var lab = new LabColor(0, 0, 0);

        lab.L = 75.5;
        lab.A = -10.3;
        lab.B = 42.1;

        lab.L.Should().Be(75.5);
        lab.A.Should().Be(-10.3);
        lab.B.Should().Be(42.1);
    }

    [Fact]
    public void DistanceTo_SamePoint_IsZero() {
        var lab = new LabColor(50.0, 25.0, -30.0);

        lab.DistanceTo(lab).Should().BeApproximately(0.0, 1e-10);
    }

    [Fact]
    public void DistanceTo_TwoIdenticalColors_IsZero() {
        var lab1 = new LabColor(50.0, 25.0, -30.0);
        var lab2 = new LabColor(50.0, 25.0, -30.0);

        lab1.DistanceTo(lab2).Should().BeApproximately(0.0, 1e-10);
    }

    [Fact]
    public void DistanceTo_IsSymmetric() {
        var lab1 = new LabColor(10.0, 20.0, 30.0);
        var lab2 = new LabColor(40.0, -10.0, 60.0);

        lab1.DistanceTo(lab2).Should().BeApproximately(lab2.DistanceTo(lab1), 1e-10);
    }

    [Fact]
    public void DistanceTo_WithKnownValues_IsCorrect() {
        var lab1 = new LabColor(0.0, 0.0, 0.0);
        var lab2 = new LabColor(3.0, 4.0, 0.0);

        lab1.DistanceTo(lab2).Should().BeApproximately(5.0, 1e-10);
    }

    [Fact]
    public void DistanceTo_WithOnlyLDifference_IsCorrect() {
        var lab1 = new LabColor(0.0, 0.0, 0.0);
        var lab2 = new LabColor(100.0, 0.0, 0.0);

        lab1.DistanceTo(lab2).Should().BeApproximately(100.0, 1e-10);
    }

    [Fact]
    public void DistanceTo_WithAllAxesOffset_IsCorrect() {
        var lab1 = new LabColor(0.0, 0.0, 0.0);
        var lab2 = new LabColor(1.0, 1.0, 1.0);

        lab1.DistanceTo(lab2).Should().BeApproximately(Math.Sqrt(3.0), 1e-10);
    }

    [Fact]
    public void ToString_ReturnsFormattedString() {
        var lab = new LabColor(50.123, -20.456, 30.789);

        lab.ToString().Should().Be("Lab(50.12, -20.46, 30.79)");
    }

    [Fact]
    public void ToString_WithZeroValues_ReturnsZeroedString() {
        var lab = new LabColor(0.0, 0.0, 0.0);

        lab.ToString().Should().Be("Lab(0.00, 0.00, 0.00)");
    }

    [Fact]
    public void ToString_IsOverridden() {
        var lab = new LabColor(50.0, 25.0, -30.0);
        var asObject = (object) lab;

        asObject.ToString().Should().Be(lab.ToString());
    }
}
