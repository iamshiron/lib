using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text;
using Shiron.Lib.Tess.ThreeMF.Internal;

namespace Shiron.Lib.Tess.ThreeMF.Tests;

public class ThreeMFDeserializeTests {
    const string ModelPartPath = "3D/3dmodel.model";

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

    const string InvalidReferencesModel = """
        <?xml version="1.0" encoding="UTF-8"?>
        <model xmlns="http://schemas.microsoft.com/3dmanufacturing/core/2015/02">
          <resources>
            <object id="1" type="model">
              <mesh>
                <vertices>
                  <vertex x="0" y="0" z="0" />
                </vertices>
                <triangles>
                  <triangle v1="0" v2="9" v3="2" />
                </triangles>
              </mesh>
            </object>
          </resources>
          <build>
            <item objectid="42" />
          </build>
        </model>
        """;

    [Fact]
    public void Deserialize_ValidPackage_DetectsExactThreeMFDocument() {
        using var stream = CreateValidArchive();

        var document = ThreeMFSerializer.Deserialize(stream);

        var exact = Assert.IsType<ThreeMFDocument>(document);
        Assert.Equal(ModelPartPath, exact.Core.PartPath);
        Assert.Same(exact.Core.MainModel, Assert.Single(exact.Core.Models));
        Assert.Single(exact.Core.MainModel.Build.Items);
        Assert.True(exact.Package.Contains(ModelPartPath));
        Assert.Same(exact.Package.Files, exact.Files);
        Assert.Contains("Metadata/thumbnail.png", exact.Files.Keys);
    }

    [Fact]
    public void Deserialize_GenericOverload_ValidDocument_ReturnsDocument() {
        using var stream = CreateValidArchive();

        var document = ThreeMFSerializer.Deserialize<ThreeMFDocument>(stream);

        Assert.IsType<ThreeMFDocument>(document);
        Assert.Equal(ModelPartPath, document.Core.PartPath);
    }

    [Fact]
    public void Deserialize_IncompatibleRequestedType_ThrowsTypeMismatch() {
        using var stream = CreateValidArchive();

        var exception = Assert.Throws<ThreeMFDocumentTypeMismatchException>(
            () => ThreeMFSerializer.Deserialize<VendorXDocument>(stream)
        );

        Assert.Equal(typeof(VendorXDocument), exception.RequestedType);
        Assert.Equal(typeof(ThreeMFDocument), exception.ActualType);
        Assert.Contains(nameof(VendorXDocument), exception.Message);
    }

    [Fact]
    public void Deserialize_BaseTypeRequest_PreservesRuntimeType() {
        var options = CreateOptions(
            new StubHandler(typeof(VendorXDocument), ThreeMFConfidence.Authoritative, new VendorXDocument())
        );

        using var stream = CreateValidArchive();
        var document = ThreeMFSerializer.Deserialize(stream, options);
        Assert.IsType<VendorXDocument>(document);

        using var typedStream = CreateValidArchive();
        var typed = ThreeMFSerializer.Deserialize<ThreeMFDocument>(typedStream, options);
        Assert.IsType<VendorXDocument>(typed);
    }

    [Fact]
    public void Deserialize_UnrecognizedPackage_ThrowsDetectionException() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["readme.txt"] = "not a 3mf"u8.ToArray(),
        });

        var exception = Assert.Throws<ThreeMFDocumentDetectionException>(
            () => ThreeMFSerializer.Deserialize(stream)
        );

        Assert.Contains("handler", exception.Message);
    }

    [Fact]
    public void Deserialize_EqualAuthorityUnrelatedHandlers_ThrowAmbiguityException() {
        var options = CreateOptions(
            new StubHandler(typeof(VendorXDocument), ThreeMFConfidence.Authoritative, new VendorXDocument()),
            new StubHandler(typeof(VendorYDocument), ThreeMFConfidence.Authoritative, new VendorYDocument())
        );

        using var stream = CreateValidArchive();

        var exception = Assert.Throws<ThreeMFAmbiguousDocumentTypeException>(
            () => ThreeMFSerializer.Deserialize(stream, options)
        );

        Assert.IsAssignableFrom<ThreeMFDocumentDetectionException>(exception);
        Assert.Equal([typeof(VendorXDocument), typeof(VendorYDocument)], exception.CandidateTypes);
    }

    [Fact]
    public void Deserialize_DuplicateHandlersForSameDocumentType_ThrowAmbiguityException() {
        var options = CreateOptions(
            new StubHandler(typeof(VendorXDocument), ThreeMFConfidence.Authoritative, new VendorXDocument()),
            new StubHandler(typeof(VendorXDocument), ThreeMFConfidence.Authoritative, new VendorXDocument())
        );

        using var stream = CreateValidArchive();

        Assert.Throws<ThreeMFAmbiguousDocumentTypeException>(
            () => ThreeMFSerializer.Deserialize(stream, options)
        );
    }

    [Fact]
    public void Deserialize_LowerConfidenceHandler_LosesAgainstAuthoritativeStandard() {
        var options = CreateOptions(
            new StubHandler(typeof(VendorXDocument), ThreeMFConfidence.Likely, new VendorXDocument())
        );

        using var stream = CreateValidArchive();

        var document = ThreeMFSerializer.Deserialize(stream, options);

        Assert.IsType<ThreeMFDocument>(document);
    }

    [Fact]
    public void Deserialize_PreserveUnknownFilesFalse_KeepsOnlyKnownParts() {
        using var stream = CreateValidArchive();
        var options = ThreeMFSerializerOptions.Default with { PreserveUnknownFiles = false };

        var document = ThreeMFSerializer.Deserialize(stream, options);

        Assert.Contains("[Content_Types].xml", document.Files.Keys);
        Assert.Contains("_rels/.rels", document.Files.Keys);
        Assert.Contains(ModelPartPath, document.Files.Keys);
        Assert.DoesNotContain("Metadata/thumbnail.png", document.Files.Keys);
    }

    [Fact]
    public void Deserialize_LenientMode_ToleratesInvalidReferences() {
        var options = ThreeMFSerializerOptions.Default with { ValidationMode = ValidationMode.Lenient };
        using var stream = CreateModelOnlyArchive(InvalidReferencesModel);

        var document = ThreeMFSerializer.Deserialize(stream, options);

        var mesh = document.Core.MainModel.Resources.Objects[0].Mesh;
        Assert.NotNull(mesh);
        Assert.Equal(9, Assert.Single(mesh!.Triangles).V2);
    }

    [Fact]
    public void Deserialize_StrictMode_ThrowsOnInvalidReferences() {
        using var stream = CreateModelOnlyArchive(InvalidReferencesModel);

        Assert.Throws<ThreeMFCoreException>(
            () => ThreeMFSerializer.Deserialize(stream)
        );
    }

    [Fact]
    public void Deserialize_LeavesStreamOpen() {
        using var stream = CreateValidArchive();

        ThreeMFSerializer.Deserialize(stream);

        Assert.True(stream.CanRead);
        Assert.True(stream.CanSeek);
    }

    [Fact]
    public void Exceptions_DeriveFromCommonBases() {
        Assert.IsAssignableFrom<ThreeMFFormatException>(new ThreeMFPackageException("x"));
        Assert.IsAssignableFrom<ThreeMFFormatException>(new ThreeMFCoreException("x"));
        Assert.IsAssignableFrom<ThreeMFException>(new ThreeMFDocumentDetectionException("x"));
        Assert.IsAssignableFrom<ThreeMFException>(
            new ThreeMFDocumentTypeMismatchException(typeof(ThreeMFDocument), typeof(VendorXDocument))
        );
    }

    static ThreeMFSerializerOptions CreateOptions(params IThreeMFDocumentHandler[] handlers) {
        var extensions = new ThreeMFExtensions();

        foreach (var handler in handlers)
            extensions.Add(handler);

        return ThreeMFSerializerOptions.Default with { Extensions = extensions };
    }

    static MemoryStream CreateValidArchive() {
        return CreateArchive(new Dictionary<string, byte[]> {
            ["[Content_Types].xml"] = Encoding.UTF8.GetBytes(ContentTypesPart),
            ["_rels/.rels"] = Encoding.UTF8.GetBytes(RelationshipsPart),
            [ModelPartPath] = Encoding.UTF8.GetBytes(ValidModel),
            ["Metadata/thumbnail.png"] = [0x89, 0x50, 0x4E, 0x47],
        });
    }

    static MemoryStream CreateModelOnlyArchive(string model) {
        return CreateArchive(new Dictionary<string, byte[]> {
            ["_rels/.rels"] = Encoding.UTF8.GetBytes(RelationshipsPart),
            [ModelPartPath] = Encoding.UTF8.GetBytes(model),
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

    sealed class StubHandler(Type documentType, ThreeMFConfidence confidence, ThreeMFDocument document)
        : IThreeMFDocumentHandler {
        public Type DocumentType => documentType;

        public ThreeMFProbeResult Probe(ThreeMFParseContext context) => new(confidence);

        public ThreeMFDocument Parse(ThreeMFParseContext context) => document;
    }

    abstract class StubDocument : ThreeMFDocument {
        [SetsRequiredMembers]
        protected StubDocument() {
            var model = new Model { Resources = new Resources { Objects = [] } };

            Files = new Dictionary<string, ReadOnlyMemory<byte>>();
            Package = new ThreeMFPackage { Files = Files };
            Core = new Core { PartPath = "stub.model", Models = [model], MainModel = model };
        }
    }

    sealed class VendorXDocument : StubDocument {
        [SetsRequiredMembers]
        public VendorXDocument() { }
    }

    sealed class VendorYDocument : StubDocument {
        [SetsRequiredMembers]
        public VendorYDocument() { }
    }
}
