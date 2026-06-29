using System.Runtime.CompilerServices;
using Shiron.Lib.Types;

namespace Shiron.Lib.Types.Ext.Conversion;

public static class LabColorExtensions {
    private const double Epsilon = 216.0 / 24389.0;
    private const double Kappa = 24389.0 / 27.0;

    private static readonly double[] Xn = [0.95047, 1.0, 1.08883];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double LinearToSrgb(double c) {
        return c <= 0.0031308 ? c * 12.92 : 1.055 * Math.Pow(c, 1.0 / 2.4) - 0.055;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double LabFInv(double t) {
        var t3 = t * t * t;
        return t3 > Epsilon ? t3 : (116.0 * t - 16.0) / Kappa;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ClampToByte(double v) {
        return (byte) (v < 0 ? 0 : v > 255 ? 255 : Math.Round(v));
    }

    public static Color32 ToColor32(this LabColor lab, byte alpha = 255) {
        var fy = (lab.L + 16.0) / 116.0;
        var fx = lab.A / 500.0 + fy;
        var fz = fy - lab.B / 200.0;

        var x = Xn[0] * LabFInv(fx);
        var y = Xn[1] * LabFInv(fy);
        var z = Xn[2] * LabFInv(fz);

        var r = 3.2404542 * x - 1.5371385 * y - 0.4985314 * z;
        var g = -0.9692660 * x + 1.8760108 * y + 0.0415560 * z;
        var b = 0.0556434 * x - 0.2040259 * y + 1.0572252 * z;

        var rb = ClampToByte(LinearToSrgb(r) * 255.0);
        var gb = ClampToByte(LinearToSrgb(g) * 255.0);
        var bb = ClampToByte(LinearToSrgb(b) * 255.0);

        return new Color32(rb, gb, bb, alpha);
    }
}
