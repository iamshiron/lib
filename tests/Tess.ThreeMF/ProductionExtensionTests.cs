using System.IO.Compression;
using System.Text;
using Shiron.Lib.Tess.ThreeMF.Exceptions;
using Shiron.Lib.Tess.ThreeMF.Parsing;

namespace Shiron.Lib.Tess.ThreeMF.Tests;

// These deliberately small snippets mirror the Production Extension structure in
// .test/NeonTower.3mf; the real fixture itself is not part of the test suite.
public class ProductionExtensionTests {
    const string ModelPartPath = "3D/3dmodel.model";
    const string ObjectPartPath = "3D/Objects/object_9.model";
    const string PartRelationshipsPath = "3D/_rels/3dmodel.model.rels";

    const string PackageRelationshipsPart = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rel-model" Type="http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel" Target="/3D/3dmodel.model" />
        </Relationships>
        """;

    const string ContentTypesPart = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml" />
          <Default Extension="model" ContentType="application/vnd.ms-package.3dmanufacturing-3dmodel+xml" />
        </Types>
        """;

    const string ProductionMainModel = """
        <?xml version="1.0" encoding="UTF-8"?>
        <model unit="millimeter" xml:lang="en-US" xmlns="http://schemas.microsoft.com/3dmanufacturing/core/2015/02" xmlns:p="http://schemas.microsoft.com/3dmanufacturing/production/2015/06" requiredextensions="p">
          <metadata name="Application">BambuStudio-02.08.02.60</metadata>
          <resources>
            <object id="2" type="model">
              <components>
                <component p:path="/3D/Objects/object_9.model" objectid="1" transform="1 0 0 0 1 0 0 0 1 0 0 0" />
              </components>
            </object>
          </resources>
          <build>
            <item objectid="2" transform="1 0 0 0 1 0 0 0 1 10 20 30" />
          </build>
        </model>
        """;

    const string ObjectPart = """
        <?xml version="1.0" encoding="UTF-8"?>
        <model unit="millimeter" xml:lang="en-US" xmlns="http://schemas.microsoft.com/3dmanufacturing/core/2015/02" xmlns:p="http://schemas.microsoft.com/3dmanufacturing/production/2015/06" requiredextensions="p">
          <metadata name="BambuStudio:3mfVersion">1</metadata>
          <resources>
            <object id="1" type="model">
              <mesh>
                <vertices>
                  <vertex x="0" y="0" z="0" />
                  <vertex x="10" y="0" z="0" />
                  <vertex x="0" y="10" z="0" />
                </vertices>
                <triangles>
                  <triangle v1="0" v2="1" v3="2" />
                </triangles>
              </mesh>
            </object>
          </resources>
          <build />
        </model>
        """;

    const string PartRelationships = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Target="Objects/object_9.model" Id="rel-1" Type="http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel" />
        </Relationships>
        """;

    [Fact]
    public void Parse_ProductionComponent_ResolvesReferencedModelPart() {
        using var stream = CreateArchive(ProductionFiles());
        var package = ThreeMFSerializer.DeserializePackage(stream);

        var core = CoreParser.Parse(package);

        Assert.Equal(2, core.Models.Count);
        Assert.Same(core.MainModel, core.Models[0]);
        Assert.Equal([ModelPartPath, ObjectPartPath], core.Parts.Keys.OrderBy(path => path, StringComparer.Ordinal));
        Assert.Same(core.Parts[ObjectPartPath], core.Models[1]);

        var component = Assert.Single(core.MainModel.Resources.Objects[0].Components);
        Assert.Equal(1, component.ObjectId);
        Assert.Equal(ObjectPartPath, component.PartPath);
        Assert.False(core.MainModel.Resources.TryGetObject(1, out _));

        var external = core.Parts[ObjectPartPath];
        Assert.True(external.Resources.TryGetObject(1, out var meshObject));
        Assert.NotNull(meshObject.Mesh);
        Assert.Equal(3, meshObject.Mesh!.Vertices.Count);
        Assert.Single(meshObject.Mesh.Triangles);

        Assert.Single(core.MainModel.Build.Items);
    }

    [Fact]
    public void Parse_ProductionComponent_PartNotInPackage_Throws() {
        var files = ProductionFiles();
        ReplaceMainComponent(files, """<component p:path="/3D/Objects/ghost.model" objectid="1" />""");
        using var stream = CreateArchive(files);

        var package = ThreeMFSerializer.DeserializePackage(stream);

        var exception = Assert.Throws<ThreeMFCoreException>(() => CoreParser.Parse(package));
        Assert.Contains("not present in the package", exception.Message);
    }

    [Fact]
    public void Parse_ProductionComponent_PartNotRelationshipLinked_Throws() {
        var files = ProductionFiles();
        files.Remove(PartRelationshipsPath);
        using var stream = CreateArchive(files);

        var package = ThreeMFSerializer.DeserializePackage(stream);

        var exception = Assert.Throws<ThreeMFCoreException>(() => CoreParser.Parse(package));
        Assert.Contains("not linked by a 3D model relationship", exception.Message);
    }

    [Fact]
    public void Parse_ProductionComponent_UnknownExternalObject_Throws() {
        var files = ProductionFiles();
        ReplaceMainComponent(files, """<component p:path="/3D/Objects/object_9.model" objectid="77" />""");
        using var stream = CreateArchive(files);

        var package = ThreeMFSerializer.DeserializePackage(stream);

        var exception = Assert.Throws<ThreeMFCoreException>(() => CoreParser.Parse(package));
        Assert.Contains("unknown object 77", exception.Message);
    }

    [Fact]
    public void Parse_RelationshipCycle_Throws() {
        var files = ProductionFiles();
        files["3D/Objects/_rels/object_9.model.rels"] = Text("""
            <?xml version="1.0" encoding="UTF-8"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Target="/3D/3dmodel.model" Id="rel-back" Type="http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel" />
            </Relationships>
            """);
        using var stream = CreateArchive(files);

        var package = ThreeMFSerializer.DeserializePackage(stream);

        var exception = Assert.Throws<ThreeMFCoreException>(() => CoreParser.Parse(package));
        Assert.Contains("cycle", exception.Message);
    }

    [Fact]
    public void Parse_DuplicateRelationshipIds_Throws() {
        var files = ProductionFiles();
        files[PartRelationshipsPath] = Text("""
            <?xml version="1.0" encoding="UTF-8"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Target="Objects/object_9.model" Id="rel-1" Type="http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel" />
              <Relationship Target="/Metadata/plate_1.png" Id="rel-1" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail" />
            </Relationships>
            """);
        using var stream = CreateArchive(files);

        var package = ThreeMFSerializer.DeserializePackage(stream);

        Assert.Throws<ThreeMFPackageException>(() => CoreParser.Parse(package));
    }

    [Fact]
    public void Parse_RelationshipTargetingMissingPart_Throws() {
        var files = ProductionFiles();
        files[PartRelationshipsPath] = Text("""
            <?xml version="1.0" encoding="UTF-8"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Target="Objects/ghost.model" Id="rel-1" Type="http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel" />
            </Relationships>
            """);
        using var stream = CreateArchive(files);

        var package = ThreeMFSerializer.DeserializePackage(stream);

        var exception = Assert.Throws<ThreeMFCoreException>(() => CoreParser.Parse(package));
        Assert.Contains("3D/Objects/ghost.model", exception.Message);
    }

    [Fact]
    public void Parse_MalformedLinkedPartXml_Throws() {
        var files = ProductionFiles();
        files[ObjectPartPath] = Text(
            """<model xmlns="http://schemas.microsoft.com/3dmanufacturing/core/2015/02"><resources>"""
        );
        using var stream = CreateArchive(files);

        var package = ThreeMFSerializer.DeserializePackage(stream);

        var exception = Assert.Throws<ThreeMFCoreException>(() => CoreParser.Parse(package));
        Assert.Contains(ObjectPartPath, exception.Message);
        Assert.Contains("not well-formed", exception.Message);
    }

    [Fact]
    public void Deserialize_BambuProductionProject_RoundTripsExactTypeAndRawFiles() {
        using var stream = CreateArchive(ProductionBambuFiles());

        var first = Assert.IsType<BambuThreeMFDocument>(ThreeMFSerializer.Deserialize(stream));

        Assert.Equal(2, first.Core.Models.Count);
        Assert.True(first.Core.Parts.ContainsKey(ObjectPartPath));
        Assert.Equal("BambuStudio-02.08.02.60", first.Project.Application);
        Assert.Equal("0.2", first.Project.ProjectSettings.Values["layer_height"]);
        var plate = Assert.Single(first.Plates);
        Assert.NotNull(plate.Thumbnail);

        using var serialized = new MemoryStream();
        ThreeMFSerializer.Serialize(serialized, first);
        serialized.Position = 0;

        var second = Assert.IsType<BambuThreeMFDocument>(ThreeMFSerializer.Deserialize(serialized));

        Assert.Equal(
            first.Files.Keys.OrderBy(path => path, StringComparer.Ordinal),
            second.Files.Keys.OrderBy(path => path, StringComparer.Ordinal)
        );

        foreach (var path in first.Files.Keys) {
            Assert.True(first.Files[path].Span.SequenceEqual(second.Files[path].Span), path);
        }

        Assert.Equal(2, second.Core.Models.Count);
        Assert.True(second.Core.Parts.ContainsKey(ObjectPartPath));
        Assert.Equal("BambuStudio-02.08.02.60", second.Project.Application);
    }

    [Fact]
    public void Deserialize_PreserveUnknownFilesFalse_KeepsProductionParts() {
        var files = ProductionBambuFiles();
        files["Notes/notes.txt"] = Text("unrelated");
        using var stream = CreateArchive(files);

        var options = ThreeMFSerializerOptions.Default with { PreserveUnknownFiles = false };
        var document = Assert.IsType<BambuThreeMFDocument>(ThreeMFSerializer.Deserialize(stream, options));

        Assert.Contains(ModelPartPath, document.Files.Keys);
        Assert.Contains(ObjectPartPath, document.Files.Keys);
        Assert.Contains(PartRelationshipsPath, document.Files.Keys);
        Assert.Contains("Metadata/project_settings.config", document.Files.Keys);
        Assert.Contains("Metadata/plate_1.png", document.Files.Keys);
        Assert.DoesNotContain("Notes/notes.txt", document.Files.Keys);
    }

    [Fact]
    public void Serialize_RestrictedProductionDocument_RoundTripsSemantically() {
        using var stream = CreateArchive(ProductionBambuFiles());

        var options = ThreeMFSerializerOptions.Default with { PreserveUnknownFiles = false };
        var restricted = Assert.IsType<BambuThreeMFDocument>(ThreeMFSerializer.Deserialize(stream, options));

        using var serialized = new MemoryStream();
        ThreeMFSerializer.Serialize(serialized, restricted);
        serialized.Position = 0;

        var roundTripped = Assert.IsType<BambuThreeMFDocument>(ThreeMFSerializer.Deserialize(serialized));

        Assert.Equal(2, roundTripped.Core.Models.Count);
        Assert.True(roundTripped.Core.Parts.ContainsKey(ObjectPartPath));
    }

    static Dictionary<string, byte[]> ProductionFiles() {
        return new Dictionary<string, byte[]> {
            ["[Content_Types].xml"] = Text(ContentTypesPart),
            ["_rels/.rels"] = Text(PackageRelationshipsPart),
            [ModelPartPath] = Text(ProductionMainModel),
            [ObjectPartPath] = Text(ObjectPart),
            [PartRelationshipsPath] = Text(PartRelationships),
        };
    }

    static Dictionary<string, byte[]> ProductionBambuFiles() {
        var files = ProductionFiles();
        files["Metadata/project_settings.config"] = Text("""{ "layer_height": "0.2" }""");
        files["Metadata/plate_1.png"] = [0x89, 0x50, 0x4E, 0x47];
        return files;
    }

    static void ReplaceMainComponent(Dictionary<string, byte[]> files, string component) {
        files[ModelPartPath] = Text(ProductionMainModel.Replace(
            """<component p:path="/3D/Objects/object_9.model" objectid="1" transform="1 0 0 0 1 0 0 0 1 0 0 0" />""",
            component
        ));
    }

    static byte[] Text(string value) => Encoding.UTF8.GetBytes(value);

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
}
