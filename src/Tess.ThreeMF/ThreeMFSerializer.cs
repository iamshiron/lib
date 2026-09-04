using System.IO.Compression;
using System.Text.Json;

namespace Shiron.Lib.Tess.ThreeMF;

public static class ThreeMFSerializer {
    public static void Serialize(Stream stream, ThreeMFDocument document, ThreeMFSerializerOptions options) {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create, true);
        foreach (var (file, bytes) in document.Files) {
            var entry = zip.CreateEntry(file, options.CompressionLevel);
            using var entryStream = entry.Open();
            entryStream.Write(bytes.Span);
        }
    }

    public static ThreeMFDocument Deserialize(Stream stream) {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, true);

        var files = new Dictionary<string, ReadOnlyMemory<byte>>();

        foreach (var entry in zip.Entries) {
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            using var entryStream = entry.Open();
            using var buffer = new MemoryStream();

            entryStream.CopyTo(buffer);
            files.Add(entry.FullName, buffer.ToArray());
        }

        return new ThreeMFDocument {
            Files = files
        };
    }
}
