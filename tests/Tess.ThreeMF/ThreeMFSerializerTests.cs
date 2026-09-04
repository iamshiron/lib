using System.IO.Compression;

namespace Shiron.Lib.Tess.ThreeMF.Tests;

public class ThreeMFSerializerTests {
    private readonly ThreeMFSerializerOptions _options = ThreeMFSerializerOptions.Default;

    [Fact]
    public void Serialize_WritesAllFilesToZipArchive() {
        var document = new ThreeMFDocument {
            Files = new Dictionary<string, ReadOnlyMemory<byte>> {
                ["[Content_Types].xml"] = "<Types />"u8.ToArray(),
                ["3D/3dmodel.model"] = "<model />"u8.ToArray(),
                ["Metadata/thumbnail.png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 }
            }
        };

        using var stream = new MemoryStream();

        ThreeMFSerializer.Serialize(stream, document, _options);

        stream.Position = 0;

        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

        Assert.Equal(3, zip.Entries.Count);

        AssertEntry(zip, "[Content_Types].xml", "<Types />"u8);
        AssertEntry(zip, "3D/3dmodel.model", "<model />"u8);
        AssertEntry(zip, "Metadata/thumbnail.png", [0x89, 0x50, 0x4E, 0x47]);
    }

    [Fact]
    public void Serialize_LeavesStreamOpen() {
        var document = new ThreeMFDocument {
            Files = new Dictionary<string, ReadOnlyMemory<byte>>()
        };

        using var stream = new MemoryStream();

        ThreeMFSerializer.Serialize(stream, document, _options);

        Assert.True(stream.CanRead);
        Assert.True(stream.CanWrite);
    }

    [Fact]
    public void Deserialize_ReadsAllFilesFromZipArchive() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["[Content_Types].xml"] = "<Types />"u8.ToArray(),
            ["3D/3dmodel.model"] = "<model />"u8.ToArray(),
            ["Metadata/thumbnail.png"] = [0x89, 0x50, 0x4E, 0x47]
        });

        var document = ThreeMFSerializer.Deserialize(stream);

        Assert.Equal(3, document.Files.Count);

        Assert.Equal(
            "<Types />"u8.ToArray(),
            document.Files["[Content_Types].xml"].ToArray()
        );

        Assert.Equal(
            "<model />"u8.ToArray(),
            document.Files["3D/3dmodel.model"].ToArray()
        );

        Assert.Equal(
            new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            document.Files["Metadata/thumbnail.png"].ToArray()
        );
    }

    [Fact]
    public void Deserialize_IgnoresDirectoryEntries() {
        using var stream = new MemoryStream();

        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true)) {
            zip.CreateEntry("3D/");

            var entry = zip.CreateEntry("3D/3dmodel.model");

            using var entryStream = entry.Open();
            entryStream.Write("<model />"u8);
        }

        stream.Position = 0;

        var document = ThreeMFSerializer.Deserialize(stream);

        Assert.Single(document.Files);
        Assert.True(document.Files.ContainsKey("3D/3dmodel.model"));
        Assert.False(document.Files.ContainsKey("3D/"));
    }

    [Fact]
    public void Deserialize_LeavesStreamOpen() {
        using var stream = CreateArchive(new Dictionary<string, byte[]>());

        ThreeMFSerializer.Deserialize(stream);

        Assert.True(stream.CanRead);
    }

    [Fact]
    public void RoundTrip_PreservesFilesExactly() {
        var expected = new Dictionary<string, ReadOnlyMemory<byte>> {
            ["[Content_Types].xml"] = "<Types />"u8.ToArray(),
            ["_rels/.rels"] = "<Relationships />"u8.ToArray(),
            ["3D/3dmodel.model"] = "<model unit=\"millimeter\" />"u8.ToArray(),
            ["Metadata/thumbnail.png"] = Enumerable.Range(0, 256)
                .Select(x => (byte) x)
                .ToArray()
        };

        var original = new ThreeMFDocument {
            Files = expected
        };

        using var stream = new MemoryStream();

        ThreeMFSerializer.Serialize(stream, original, _options);

        stream.Position = 0;

        var result = ThreeMFSerializer.Deserialize(stream);

        Assert.Equal(expected.Count, result.Files.Count);

        foreach (var (path, bytes) in expected) {
            Assert.True(result.Files.TryGetValue(path, out var actual));
            Assert.Equal(bytes.ToArray(), actual.ToArray());
        }
    }

    [Fact]
    public void RoundTrip_PreservesEmptyFiles() {
        var document = new ThreeMFDocument {
            Files = new Dictionary<string, ReadOnlyMemory<byte>> {
                ["Metadata/empty.txt"] = ReadOnlyMemory<byte>.Empty
            }
        };

        using var stream = new MemoryStream();

        ThreeMFSerializer.Serialize(stream, document, _options);

        stream.Position = 0;

        var result = ThreeMFSerializer.Deserialize(stream);

        Assert.True(result.Files.ContainsKey("Metadata/empty.txt"));
        Assert.Empty(result.Files["Metadata/empty.txt"].ToArray());
    }

    private static MemoryStream CreateArchive(
        IReadOnlyDictionary<string, byte[]> files
    ) {
        var stream = new MemoryStream();

        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true)) {
            foreach (var (path, bytes) in files) {
                var entry = zip.CreateEntry(path);

                using var entryStream = entry.Open();
                entryStream.Write(bytes);
            }
        }

        stream.Position = 0;

        return stream;
    }

    private static void AssertEntry(ZipArchive archive, string path, ReadOnlySpan<byte> expected) {
        var entry = archive.GetEntry(path);

        Assert.NotNull(entry);

        using var stream = entry.Open();
        using var buffer = new MemoryStream();

        stream.CopyTo(buffer);

        Assert.Equal(expected.ToArray(), buffer.ToArray());
    }
}
