using System.Runtime.CompilerServices;
using Shiron.Lib.Types;

namespace Shiron.Lib.Types.Ext.Conversion;

public static class Color32Extensions {
    private const double Epsilon = 216.0 / 24389.0;
    private const double Kappa = 24389.0 / 27.0;

    private static readonly double[] Xn = [0.95047, 1.0, 1.08883];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double SrgbToLinear(double c) {
        return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double LabF(double t) {
        return t > Epsilon ? Math.Cbrt(t) : (Kappa * t + 16.0) / 116.0;
    }

    public static LabColor ToLabColor(this Color32 color) {
        var r = SrgbToLinear(color.R / 255.0);
        var g = SrgbToLinear(color.G / 255.0);
        var b = SrgbToLinear(color.B / 255.0);

        var x = 0.4124564 * r + 0.3575761 * g + 0.1804375 * b;
        var y = 0.2126729 * r + 0.7151522 * g + 0.0721750 * b;
        var z = 0.0193339 * r + 0.1191920 * g + 0.9503041 * b;

        var fx = LabF(x / Xn[0]);
        var fy = LabF(y / Xn[1]);
        var fz = LabF(z / Xn[2]);

        var l = 116.0 * fy - 16.0;
        var a = 500.0 * (fx - fy);
        var bv = 200.0 * (fy - fz);

        return new LabColor(l, a, bv);
    }
}
