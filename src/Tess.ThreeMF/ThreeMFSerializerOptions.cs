using System.IO.Compression;
using System.Text.Json;

namespace Shiron.Lib.Tess.ThreeMF;

public readonly record struct ThreeMFSerializerOptions() {
    public required JsonSerializerOptions JsonOptions { get; init; }
    public required CompressionLevel CompressionLevel { get; init; }

    public static ThreeMFSerializerOptions Default => new() {
        JsonOptions = new JsonSerializerOptions {
            WriteIndented = true
        },
        CompressionLevel = CompressionLevel.Optimal
    };
}
