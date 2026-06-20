using System.Runtime.CompilerServices;

namespace Shiron.Lib.Types;

public class Color32(byte r, byte g, byte b, byte a) {
    public byte R { get; set; } = r;
    public byte G { get; set; } = g;
    public byte B { get; set; } = b;
    public byte A { get; set; } = a;

    public Color32(int rgba) : this((byte) (rgba >> 24), (byte) (rgba >> 16), (byte) (rgba >> 8), (byte) rgba) { }
    public Color32(int rgb, byte a) : this((byte) (rgb >> 16), (byte) (rgb >> 8), (byte) rgb, a) { }

    public int ToRgba => R << 24 | G << 16 | B << 8 | A;
    public int ToRgb => R << 16 | G << 8 | B;

    public string ToHex() {
        return $"#{R:X2}{G:X2}{B:X2}{A:X2}";
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() {
        return ToHex();
    }
}
