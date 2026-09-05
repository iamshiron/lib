using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text;
using Shiron.Lib.Tess.ThreeMF.Exceptions;

namespace Shiron.Lib.Tess.ThreeMF.Tests;

public class ThreeMFExtensionTests {
    const string ModelPartPath = "3D/3dmodel.model";

    const string VendorZMarkerPath = "Metadata/vendor_z.json";

    const string RelationshipsPart = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rel0" Target="/3D/3dmodel.model" Type="http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel" />
        </Relationships>
        """;

    const string ContentTypesPart = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Override PartName="/3D/3dmodel.model" ContentType="application/vnd.ms-package.3dmanufacturing-3dmodel+xml" />
        </Types>
        """;

    const string ValidModel = """
        <?xml version="1.0" encoding="UTF-8"?>
        <model unit="millimeter" xmlns="http://schemas.microsoft.com/3dmanufacturing/core/2015/02">
          <resources>
            <object id="1" type="model">
              <mesh>
                <vertices>
                  <vertex x="0" y="0" z="0" />
                  <vertex x="1" y="0" z="0" />
                  <vertex x="0" y="1" z="0" />
                </vertices>
                <triangles>
                  <triangle v1="0" v2="1" v3="2" />
                </triangles>
              </mesh>
            </object>
          </resources>
          <build>
            <item objectid="1" />
          </build>
        </model>
        """;

    [Fact]
    public void Add_RegisteredExtension_WinsDetectionWithExactRuntimeType() {
        var options = CreateOptions(new VendorZExtension());

        using var stream = CreateVendorZArchive();
        var document = ThreeMFSerializer.Deserialize(stream, options);

        var exact = Assert.IsType<VendorZDocument>(document);
        Assert.Contains(VendorZMarkerPath, exact.Files.Keys);
        Assert.True(stream.CanRead);
        Assert.True(stream.CanSeek);
    }

    [Fact]
    public void Add_NullExtension_ThrowsArgumentNull() {
        var extensions = new ThreeMFExtensions();

        Assert.Throws<ArgumentNullException>(() => extensions.Add(null!));
    }

    [Fact]
    public void Add_ExtensionNotClaimingPackage_LeavesBuiltinDetectionIntact() {
        var options = CreateOptions(new VendorZExtension());

        using var stream = CreateValidArchive();
        var document = ThreeMFSerializer.Deserialize(stream, options);

        Assert.IsType<ThreeMFDocument>(document);
    }

    [Fact]
    public void Deserialize_BaseTypeRequest_PreservesExtensionDocumentType() {
        var options = CreateOptions(new VendorZExtension());

        using var stream = CreateVendorZArchive();
        var document = ThreeMFSerializer.Deserialize<ThreeMFDocument>(stream, options);

        var exact = Assert.IsType<VendorZDocument>(document);
        Assert.Contains(VendorZMarkerPath, exact.Files.Keys);
    }

    [Fact]
    public void Deserialize_IncompatibleRequestedType_ThrowsTypeMismatch() {
        var options = CreateOptions(new VendorZExtension());

        using var stream = CreateVendorZArchive();

        var exception = Assert.Throws<ThreeMFDocumentTypeMismatchException>(
            () => ThreeMFSerializer.Deserialize<BambuThreeMFDocument>(stream, options)
        );

        Assert.Equal(typeof(BambuThreeMFDocument), exception.RequestedType);
        Assert.Equal(typeof(VendorZDocument), exception.ActualType);
    }

    [Fact]
    public void Serialize_ExtensionDocument_UsesExtensionWriter() {
        var options = CreateOptions(new VendorZExtension());

        using var stream = CreateVendorZArchive();
        var document = ThreeMFSerializer.Deserialize<VendorZDocument>(stream, options);

        using var output = new MemoryStream();
        ThreeMFSerializer.Serialize(output, document, options);

        var files = ReadArchive(output);
        Assert.Equal(
            VendorZExtension.WriterStamp,
            Encoding.UTF8.GetString(files[VendorZMarkerPath])
        );
        Assert.True(output.CanRead);
        Assert.True(output.CanSeek);
    }

    [Fact]
    public void Serialize_ExtensionWithoutCustomWriter_UsesDefaultRawFileWriter() {
        var options = CreateOptions(new VendorWExtension());

        using var stream = CreateVendorWArchive();
        var document = ThreeMFSerializer.Deserialize<VendorWDocument>(stream, options);

        using var output = new MemoryStream();
        ThreeMFSerializer.Serialize(output, document, options);

        var files = ReadArchive(output);
        Assert.Equal(Encoding.UTF8.GetBytes(ContentTypesPart), files[ThreeMFSerializer.ContentTypesPath]);
        Assert.Equal(Encoding.UTF8.GetBytes(RelationshipsPart), files[ThreeMFSerializer.RelationshipsPath]);
        Assert.Equal(Encoding.UTF8.GetBytes(ValidModel), files[ModelPartPath]);
        Assert.Equal(Encoding.UTF8.GetBytes(VendorWExtension.MarkerContent), files[VendorWExtension.MarkerPath]);
    }

    static ThreeMFSerializerOptions CreateOptions(params IThreeMFExtension[] extensions) {
        var container = new ThreeMFExtensions();

        foreach (var extension in extensions)
            container.Add(extension);

        return ThreeMFSerializerOptions.Default with { Extensions = container };
    }

    static MemoryStream CreateValidArchive() {
        return CreateArchive(new Dictionary<string, byte[]> {
            ["[Content_Types].xml"] = Encoding.UTF8.GetBytes(ContentTypesPart),
            ["_rels/.rels"] = Encoding.UTF8.GetBytes(RelationshipsPart),
            [ModelPartPath] = Encoding.UTF8.GetBytes(ValidModel),
        });
    }

    static MemoryStream CreateVendorZArchive() {
        return CreateArchive(new Dictionary<string, byte[]> {
            ["[Content_Types].xml"] = Encoding.UTF8.GetBytes(ContentTypesPart),
            ["_rels/.rels"] = Encoding.UTF8.GetBytes(RelationshipsPart),
            [ModelPartPath] = Encoding.UTF8.GetBytes(ValidModel),
            [VendorZMarkerPath] = "{}"u8.ToArray(),
        });
    }

    static MemoryStream CreateVendorWArchive() {
        return CreateArchive(new Dictionary<string, byte[]> {
            ["[Content_Types].xml"] = Encoding.UTF8.GetBytes(ContentTypesPart),
            ["_rels/.rels"] = Encoding.UTF8.GetBytes(RelationshipsPart),
            [ModelPartPath] = Encoding.UTF8.GetBytes(ValidModel),
            [VendorWExtension.MarkerPath] = Encoding.UTF8.GetBytes(VendorWExtension.MarkerContent),
        });
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

    static Dictionary<string, byte[]> ReadArchive(Stream stream) {
        stream.Position = 0;

        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, true);

        return zip.Entries.ToDictionary(
            entry => entry.FullName,
            entry => ReadEntry(entry)
        );
    }

    static byte[] ReadEntry(ZipArchiveEntry entry) {
        using var entryStream = entry.Open();
        using var buffer = new MemoryStream();

        entryStream.CopyTo(buffer);

        return buffer.ToArray();
    }

    sealed class VendorZExtension : IThreeMFExtension {
        public const string WriterStamp = "written by the VendorZ extension writer";

        public Type DocumentType => typeof(VendorZDocument);

        public ThreeMFProbeResult Probe(ThreeMFProbeContext context) {
            return context.Files.ContainsKey(VendorZMarkerPath)
                ? ThreeMFProbeResult.Certain
                : ThreeMFProbeResult.NoMatch;
        }

        public ThreeMFDocument Parse(ThreeMFParseContext context) => new VendorZDocument(context.Files);

        public void Write(Stream stream, ThreeMFDocument document, ThreeMFSerializerOptions options) {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Create, true);

            var entry = zip.CreateEntry(VendorZMarkerPath);

            using var entryStream = entry.Open();
            entryStream.Write(Encoding.UTF8.GetBytes(WriterStamp));
        }
    }

    sealed class VendorWExtension : IThreeMFExtension {
        public const string MarkerPath = "Metadata/vendor_w.json";
        public const string MarkerContent = """{ "vendor": "W" }""";

        public Type DocumentType => typeof(VendorWDocument);

        public ThreeMFProbeResult Probe(ThreeMFProbeContext context) {
            return context.Files.ContainsKey(MarkerPath)
                ? ThreeMFProbeResult.Certain
                : ThreeMFProbeResult.NoMatch;
        }

        public ThreeMFDocument Parse(ThreeMFParseContext context) => new VendorWDocument(context.Files);
    }

    abstract class VendorDocumentBase : ThreeMFDocument {
        [SetsRequiredMembers]
        protected VendorDocumentBase(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) {
            var model = new Model { Resources = new Resources { Objects = [] } };

            Files = files;
            Package = new ThreeMFPackage { Files = files };
            Core = new Core { PartPath = ModelPartPath, Models = [model], MainModel = model };
        }
    }

    sealed class VendorZDocument : VendorDocumentBase {
        [SetsRequiredMembers]
        public VendorZDocument(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) : base(files) { }
    }

    sealed class VendorWDocument : VendorDocumentBase {
        [SetsRequiredMembers]
        public VendorWDocument(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) : base(files) { }
    }
}
