using System.Runtime.CompilerServices;

namespace Shiron.Lib.Types;

public class LabColor(double l, double a, double b) {
    public double L { get; set; } = l;
    public double A { get; set; } = a;
    public double B { get; set; } = b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double DistanceTo(LabColor other) {
        var dl = L - other.L;
        var da = A - other.A;
        var db = B - other.B;
        return Math.Sqrt(dl * dl + da * da + db * db);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() {
        return $"Lab({L:F2}, {A:F2}, {B:F2})";
    }
}
