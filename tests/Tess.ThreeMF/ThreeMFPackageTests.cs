using System.IO.Compression;
using System.Text;
using Shiron.Lib.Tess.ThreeMF.Exceptions;
using Shiron.Lib.Tess.ThreeMF.Opc;

namespace Shiron.Lib.Tess.ThreeMF.Tests;

public class ThreeMFPackageTests {
    [Fact]
    public void Deserialize_ParsesContentTypes() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["[Content_Types].xml"] = """
                <?xml version="1.0" encoding="UTF-8"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml" />
                  <Default Extension="model" ContentType="application/vnd.ms-package.3dmanufacturing-3dmodel+xml" />
                  <Override PartName="/3D/3dmodel.model" ContentType="application/vnd.ms-package.3dmanufacturing-3dmodel+xml" />
                </Types>
                """u8.ToArray(),
            ["3D/3dmodel.model"] = "<model />"u8.ToArray(),
        });

        var package = ThreeMFSerializer.DeserializePackage(stream);

        Assert.Equal(3, package.ContentTypes.Count);

        Assert.Equal(
            new ContentType {
                Extension = "rels",
                MediaType = "application/vnd.openxmlformats-package.relationships+xml",
            },
            package.ContentTypes[0]
        );

        Assert.Equal(
            new ContentType {
                Extension = "model",
                MediaType = "application/vnd.ms-package.3dmanufacturing-3dmodel+xml",
            },
            package.ContentTypes[1]
        );

        var overrideType = package.ContentTypes[2];

        Assert.Equal(
            new ContentType {
                PartName = "/3D/3dmodel.model",
                MediaType = "application/vnd.ms-package.3dmanufacturing-3dmodel+xml",
            },
            overrideType
        );
        Assert.True(overrideType.IsOverride);
        Assert.Null(overrideType.Extension);
        Assert.False(package.ContentTypes[0].IsOverride);
    }

    [Fact]
    public void Deserialize_ParsesRelationships() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["[Content_Types].xml"] = "<Types />"u8.ToArray(),
            ["_rels/.rels"] = """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rel0" Target="/3D/3dmodel.model" Type="http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel" />
                  <Relationship Id="rel1" Target="/Metadata/thumbnail.png" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail" />
                </Relationships>
                """u8.ToArray(),
        });

        var package = ThreeMFSerializer.DeserializePackage(stream);

        Assert.Equal(2, package.Relationships.Count);

        Assert.Equal(
            new Relationship {
                Id = "rel0",
                Type = "http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel",
                Target = "/3D/3dmodel.model",
            },
            package.Relationships[0]
        );

        Assert.Equal(
            new Relationship {
                Id = "rel1",
                Type = "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail",
                Target = "/Metadata/thumbnail.png",
            },
            package.Relationships[1]
        );
    }

    [Fact]
    public void Deserialize_MissingMetadataParts_YieldsEmptyCollections() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["3D/3dmodel.model"] = "<model />"u8.ToArray(),
        });

        var package = ThreeMFSerializer.DeserializePackage(stream);

        Assert.Single(package.Files);
        Assert.Empty(package.ContentTypes);
        Assert.Empty(package.Relationships);
    }

    [Fact]
    public void Deserialize_EmptyMetadataElements_YieldEmptyCollections() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["[Content_Types].xml"] = "<Types />"u8.ToArray(),
            ["_rels/.rels"] = "<Relationships />"u8.ToArray(),
        });

        var package = ThreeMFSerializer.DeserializePackage(stream);

        Assert.Empty(package.ContentTypes);
        Assert.Empty(package.Relationships);
    }

    [Fact]
    public void RoundTrip_PreservesParsedMetadata() {
        var package = new ThreeMFPackage {
            Files = new Dictionary<string, ReadOnlyMemory<byte>> {
                ["[Content_Types].xml"] = """
                    <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                      <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml" />
                      <Override PartName="/3D/3dmodel.model" ContentType="application/vnd.ms-package.3dmanufacturing-3dmodel+xml" />
                    </Types>
                    """u8.ToArray(),
                ["_rels/.rels"] = """
                    <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                      <Relationship Id="rel0" Target="/3D/3dmodel.model" Type="http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel" />
                    </Relationships>
                    """u8.ToArray(),
            },
        };

        using var stream = new MemoryStream();

        ThreeMFSerializer.SerializePackage(stream, package, ThreeMFSerializerOptions.Default);
        stream.Position = 0;

        var result = ThreeMFSerializer.DeserializePackage(stream);

        Assert.Equal(
            new ContentType[] {
                new() {
                    Extension = "rels",
                    MediaType = "application/vnd.openxmlformats-package.relationships+xml",
                },
                new() {
                    PartName = "/3D/3dmodel.model",
                    MediaType = "application/vnd.ms-package.3dmanufacturing-3dmodel+xml",
                },
            },
            result.ContentTypes
        );

        Assert.Equal(
            new Relationship[] {
                new() {
                    Id = "rel0",
                    Type = "http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel",
                    Target = "/3D/3dmodel.model",
                },
            },
            result.Relationships
        );
    }

    [Fact]
    public void Deserialize_PreservesEmptyAndNestedFiles() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["3D/nested/deep/empty.bin"] = [],
            ["Metadata/thumbnail.png"] = [0x89, 0x50, 0x4E, 0x47],
        });

        var package = ThreeMFSerializer.DeserializePackage(stream);

        Assert.Equal(2, package.Files.Count);

        Assert.True(package.TryGet("3D/nested/deep/empty.bin", out var empty));
        Assert.Empty(empty.ToArray());

        Assert.True(package.Contains("Metadata/thumbnail.png"));
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], package.Get("Metadata/thumbnail.png").ToArray());

        Assert.False(package.Contains("3D/missing.model"));
        Assert.False(package.TryGet("3D/missing.model", out _));
    }

    [Fact]
    public void Deserialize_DuplicateFilePaths_ThrowsPackageException() {
        using var stream = new MemoryStream();

        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true)) {
            foreach (var content in new[] { "<model1 />"u8.ToArray(), "<model2 />"u8.ToArray() }) {
                var entry = zip.CreateEntry("3D/3dmodel.model");

                using var entryStream = entry.Open();
                entryStream.Write(content);
            }
        }

        stream.Position = 0;

        var exception = Assert.Throws<ThreeMFPackageException>(
            () => ThreeMFSerializer.DeserializePackage(stream)
        );

        Assert.Contains("3D/3dmodel.model", exception.Message);
    }

    [Fact]
    public void Deserialize_NotAZipArchive_ThrowsPackageException() {
        using var stream = new MemoryStream("this is definitely not a zip archive"u8.ToArray());

        Assert.Throws<ThreeMFPackageException>(
            () => ThreeMFSerializer.DeserializePackage(stream)
        );
    }

    [Fact]
    public void Deserialize_EmptyStream_ThrowsPackageException() {
        using var stream = new MemoryStream();

        Assert.Throws<ThreeMFPackageException>(
            () => ThreeMFSerializer.DeserializePackage(stream)
        );
    }

    [Fact]
    public void Deserialize_CorruptEntryData_ThrowsPackageException() {
        using var stream = new MemoryStream();

        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true)) {
            var entry = zip.CreateEntry("3D/3dmodel.model");

            using var entryStream = entry.Open();
            entryStream.Write(Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("<vertex x=\"1\" />", 512))));
        }

        CorruptByte(stream, 64);

        stream.Position = 0;

        var exception = Assert.Throws<ThreeMFPackageException>(
            () => ThreeMFSerializer.DeserializePackage(stream)
        );

        Assert.Contains("3D/3dmodel.model", exception.Message);
    }

    [Fact]
    public void Deserialize_MalformedContentTypes_ThrowsPackageException() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["[Content_Types].xml"] = "this is not xml at all"u8.ToArray(),
        });

        var exception = Assert.Throws<ThreeMFPackageException>(
            () => ThreeMFSerializer.DeserializePackage(stream)
        );

        Assert.Contains("[Content_Types].xml", exception.Message);
    }

    [Fact]
    public void Deserialize_WrongContentTypesRoot_ThrowsPackageException() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["[Content_Types].xml"] = "<NotTypes />"u8.ToArray(),
        });

        var exception = Assert.Throws<ThreeMFPackageException>(
            () => ThreeMFSerializer.DeserializePackage(stream)
        );

        Assert.Contains("<Types>", exception.Message);
    }

    [Fact]
    public void Deserialize_ContentTypesMissingRequiredAttribute_ThrowsPackageException() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["[Content_Types].xml"] =
                """<Types><Default Extension="rels" /></Types>"""u8.ToArray(),
        });

        var exception = Assert.Throws<ThreeMFPackageException>(
            () => ThreeMFSerializer.DeserializePackage(stream)
        );

        Assert.Contains("ContentType", exception.Message);
    }

    [Fact]
    public void Deserialize_ContentTypesOverrideWithoutPartName_ThrowsPackageException() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["[Content_Types].xml"] =
                """<Types><Override ContentType="application/test" /></Types>"""u8.ToArray(),
        });

        var exception = Assert.Throws<ThreeMFPackageException>(
            () => ThreeMFSerializer.DeserializePackage(stream)
        );

        Assert.Contains("PartName", exception.Message);
    }

    [Fact]
    public void Deserialize_MalformedRelationships_ThrowsPackageException() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["_rels/.rels"] = "<Relationships>"u8.ToArray(),
        });

        var exception = Assert.Throws<ThreeMFPackageException>(
            () => ThreeMFSerializer.DeserializePackage(stream)
        );

        Assert.Contains("_rels/.rels", exception.Message);
    }

    [Fact]
    public void Deserialize_WrongRelationshipsRoot_ThrowsPackageException() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["_rels/.rels"] = "<Other />"u8.ToArray(),
        });

        var exception = Assert.Throws<ThreeMFPackageException>(
            () => ThreeMFSerializer.DeserializePackage(stream)
        );

        Assert.Contains("<Relationships>", exception.Message);
    }

    [Fact]
    public void Deserialize_RelationshipMissingTarget_ThrowsPackageException() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["_rels/.rels"] =
                """<Relationships><Relationship Id="rel0" Type="http://example.com/rel" /></Relationships>"""u8.ToArray(),
        });

        var exception = Assert.Throws<ThreeMFPackageException>(
            () => ThreeMFSerializer.DeserializePackage(stream)
        );

        Assert.Contains("Target", exception.Message);
    }

    [Fact]
    public void Deserialize_RelationshipWithEmptyId_ThrowsPackageException() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["_rels/.rels"] =
                """<Relationships><Relationship Id="" Type="http://example.com/rel" Target="/3D/3dmodel.model" /></Relationships>"""u8.ToArray(),
        });

        var exception = Assert.Throws<ThreeMFPackageException>(
            () => ThreeMFSerializer.DeserializePackage(stream)
        );

        Assert.Contains("Id", exception.Message);
    }

    [Fact]
    public void Deserialize_LeavesStreamOpen() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["[Content_Types].xml"] = "<Types />"u8.ToArray(),
            ["_rels/.rels"] = "<Relationships />"u8.ToArray(),
        });

        ThreeMFSerializer.DeserializePackage(stream);

        Assert.True(stream.CanRead);
        Assert.True(stream.CanSeek);
    }

    static MemoryStream CreateArchive(IReadOnlyDictionary<string, byte[]> files) {
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

    static void CorruptByte(MemoryStream stream, long position) {
        var previous = stream.Position;

        stream.Position = position;
        stream.WriteByte(0xFF);
        stream.Position = previous;
    }
}
