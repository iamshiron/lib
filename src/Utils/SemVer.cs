using System.Diagnostics.CodeAnalysis;
using Silk.NET.Core;

namespace Shiron.Lib.Utils;

public readonly record struct SemVer : ISpanFormattable, IComparable<SemVer> {
    public uint Major { get; init; }
    public uint Minor { get; init; }
    public uint Patch { get; init; }

    public SemVer(uint major, uint minor, uint patch) {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public SemVer(uint packedVersion) {
        Major = packedVersion >> 22 & 0x7F;
        Minor = packedVersion >> 12 & 0x3FF;
        Patch = packedVersion & 0xFFF;
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
        return destination.TryWrite($"{Major}.{Minor}.{Patch}", out charsWritten);
    }

    public override string ToString() {
        return $"{Major}.{Minor}.{Patch}";
    }
    public string ToString(string? format, IFormatProvider? formatProvider) {
        return ToString();
    }

    public static implicit operator Version32(SemVer v) {
        return new Version32(v.Major, v.Minor, v.Patch);
    }
    public static implicit operator SemVer(Version32 v) {
        return new SemVer(v.Major, v.Minor, v.Patch);
    }

    public static implicit operator uint(SemVer v) {
        return (v.Major & 0x7F) << 22 | (v.Minor & 0x3FF) << 12 | v.Patch & 0xFFF;
    }

    public int CompareTo(SemVer other) {
        if (Major != other.Major) {
            return Major.CompareTo(other.Major);
        }
        if (Minor != other.Minor) {
            return Minor.CompareTo(other.Minor);
        }
        return Patch.CompareTo(other.Patch);
    }

    public static bool operator <(SemVer left, SemVer right) {
        return left.CompareTo(right) < 0;
    }
    public static bool operator >(SemVer left, SemVer right) {
        return left.CompareTo(right) > 0;
    }
    public static bool operator <=(SemVer left, SemVer right) {
        return left.CompareTo(right) <= 0;
    }
    public static bool operator >=(SemVer left, SemVer right) {
        return left.CompareTo(right) >= 0;
    }

    public static SemVer Parse(ReadOnlySpan<char> input) {
        if (TryParse(input, out var result)) {
            return result;
        }
        throw new FormatException($"Invalid SemVer string: '{input.ToString()}'");
    }

    public static bool TryParse(ReadOnlySpan<char> input, out SemVer result) {
        result = default;

        var firstDot = input.IndexOf('.');
        if (firstDot < 0 || !uint.TryParse(input[..firstDot], out var major)) {
            return false;
        }
        input = input[(firstDot + 1)..];

        var secondDot = input.IndexOf('.');
        if (secondDot < 0 || !uint.TryParse(input[..secondDot], out var minor)) {
            return false;
        }
        input = input[(secondDot + 1)..];

        if (!uint.TryParse(input, out var patch)) {
            return false;
        }

        result = new SemVer(major, minor, patch);
        return true;
    }

    public static SemVer Parse(string input) {
        if (TryParse(input.AsSpan(), out var result)) {
            return result;
        }
        throw new FormatException($"Invalid SemVer string: '{input}'");
    }
}
