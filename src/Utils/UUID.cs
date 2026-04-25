using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Shiron.Lib.Utils;

/// <summary>
/// Represents a Universally Unique Identifier (UUID) as defined by RFC 4122.
/// </summary>
/// <remarks>
/// Note: This UUID is NOT cryptographically secure and should not be used for security-critical applications.
/// </remarks>
public readonly struct UUID : IEquatable<UUID>, ISpanFormattable {
    private readonly ulong _upper;
    private readonly ulong _lower;

    /// <summary>
    /// Create a new UUIDv4 from the upper and lower bytes
    /// </summary>
    /// <remarks>
    /// Note: No check is performed to ensure the UUID is valid according to RFC 4122.
    /// </remarks>
    public UUID(ulong upper, ulong lower) {
        _upper = upper;
        _lower = lower;
    }

    /// <summary>
    /// Generates a new random UUIDv4 based on RFC 4122 specifications.
    /// </summary>
    /// <returns>A randomly generated UUIDv4.</returns>
    public static UUID Random() {
        Span<byte> bytes = stackalloc byte[16];
        System.Random.Shared.NextBytes(bytes);

        // RFC Compliance
        bytes[7] = (byte) (bytes[7] & 0x0F | 0x40);
        bytes[8] = (byte) (bytes[8] & 0x3F | 0x80);

        ReadOnlySpan<ulong> data = MemoryMarshal.Cast<byte, ulong>(bytes);
        return new UUID(data[0], data[1]);
    }

    public override string ToString() {
        return ToString(null, null);
    }
    public string ToString(string? format, IFormatProvider? formatProvider) {
        return string.Create(36, this, static (span, uuid) => {
            uuid.TryFormat(span, out _, default, null);
        });
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
        if (destination.Length < 36) {
            charsWritten = 0;
            return false;
        }

        // Reinterpret the ulongs as bytes directly on the stack
        Span<ulong> ulongs = stackalloc ulong[] { _upper, _lower };
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes<ulong>(ulongs);

        WriteHexByte(bytes[0], destination, 0);
        WriteHexByte(bytes[1], destination, 2);
        WriteHexByte(bytes[2], destination, 4);
        WriteHexByte(bytes[3], destination, 6);
        destination[8] = '-';
        WriteHexByte(bytes[4], destination, 9);
        WriteHexByte(bytes[5], destination, 11);
        destination[13] = '-';
        WriteHexByte(bytes[6], destination, 14);
        WriteHexByte(bytes[7], destination, 16);
        destination[18] = '-';
        WriteHexByte(bytes[8], destination, 19);
        WriteHexByte(bytes[9], destination, 21);
        destination[23] = '-';
        WriteHexByte(bytes[10], destination, 24);
        WriteHexByte(bytes[11], destination, 26);
        WriteHexByte(bytes[12], destination, 28);
        WriteHexByte(bytes[13], destination, 30);
        WriteHexByte(bytes[14], destination, 32);
        WriteHexByte(bytes[15], destination, 34);

        charsWritten = 36;
        return true;
    }

    private static void WriteHexByte(byte b, Span<char> dest, int offset) {
        dest[offset] = HexToChar(b >> 4);
        dest[offset + 1] = HexToChar(b & 0x0F);
    }

    private static char HexToChar(int value) {
        return (char) (value < 10 ? value + '0' : value - 10 + 'a');
    }

    /// <summary>
    /// Converts a string representation of a UUID to its UUID struct equivalent.
    /// </summary>
    /// <param name="value">The string representation of the UUID.</param>
    /// <returns>The UUID parsed from the string.</returns>
    /// <exception cref="FormatException">Thrown when the string does not match the expected UUID format.</exception>
    public static UUID FromString(ReadOnlySpan<char> value) {
        if (value.Length != 36 || value[8] != '-' || value[13] != '-' || value[18] != '-' || value[23] != '-') {
            throw new FormatException("Invalid UUID string format.");
        }

        Span<byte> bytes = stackalloc byte[16];

        bytes[0] = ParseHexByte(value, 0);
        bytes[1] = ParseHexByte(value, 2);
        bytes[2] = ParseHexByte(value, 4);
        bytes[3] = ParseHexByte(value, 6);
        bytes[4] = ParseHexByte(value, 9);
        bytes[5] = ParseHexByte(value, 11);
        bytes[6] = ParseHexByte(value, 14);
        bytes[7] = ParseHexByte(value, 16);
        bytes[8] = ParseHexByte(value, 19);
        bytes[9] = ParseHexByte(value, 21);
        bytes[10] = ParseHexByte(value, 24);
        bytes[11] = ParseHexByte(value, 26);
        bytes[12] = ParseHexByte(value, 28);
        bytes[13] = ParseHexByte(value, 30);
        bytes[14] = ParseHexByte(value, 32);
        bytes[15] = ParseHexByte(value, 34);

        ReadOnlySpan<ulong> data = MemoryMarshal.Cast<byte, ulong>(bytes);
        return new UUID(data[0], data[1]);
    }

    private static byte ParseHexByte(ReadOnlySpan<char> str, int offset) {
        return (byte) (CharToHex(str[offset]) << 4 | CharToHex(str[offset + 1]));
    }

    private static int CharToHex(char c) {
        if ((uint) (c - '0') <= 9) {
            return c - '0';
        }
        if ((uint) (c - 'a') <= 5) {
            return c - 'a' + 10;
        }
        if ((uint) (c - 'A') <= 5) {
            return c - 'A' + 10;
        }

        throw new FormatException($"Invalid hex character: {c}");
    }

    public bool Equals(UUID other) {
        return _upper == other._upper && _lower == other._lower;
    }
    public override bool Equals([NotNullWhen(true)] object? obj) {
        return obj is UUID uuid && Equals(uuid);
    }
    public override int GetHashCode() {
        return HashCode.Combine(_upper, _lower);
    }
    public static bool operator ==(UUID left, UUID right) {
        return left.Equals(right);
    }
    public static bool operator !=(UUID left, UUID right) {
        return !left.Equals(right);
    }
}
