using System.IO.Compression;
using System.Text;
using Shiron.Lib.Tess.ThreeMF.Writing;

namespace Shiron.Lib.Tess.ThreeMF.Tests;

public class ThreeMFSerializeTests {
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
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml" />
          <Override PartName="/3D/3dmodel.model" ContentType="application/vnd.ms-package.3dmanufacturing-3dmodel+xml" />
        </Types>
        """;

    const string GenericModel = """
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

    const string BambuModel = """
        <?xml version="1.0" encoding="UTF-8"?>
        <model unit="millimeter" xmlns="http://schemas.microsoft.com/3dmanufacturing/core/2015/02">
          <metadata name="Application">BambuStudio 01.09.05.51</metadata>
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
          <build>
            <item objectid="1" />
          </build>
        </model>
        """;

    const string HeaderItemPart = """
        X-BBL-Client-Type = bambu-studio
        X-BBL-Client-Version = 01.09.05.51
        """;

    const string ProjectSettingsPart = """
        {
          "layer_height": "0.2",
          "nozzle_temperature": 220,
          "enable_bed": true
        }
        """;

    const string ModelSettingsPart = """
        <?xml version="1.0" encoding="UTF-8"?>
        <config>
          <plate index="1" name="Default Plate" objects="1 2">
            <metadata type="printer_model_id">X1C</metadata>
          </plate>
          <plate index="2">
            <metadata type="plate_name">Second Plate</metadata>
            <object id="3" name="Cube" />
          </plate>
        </config>
        """;

    [Fact]
    public void Serialize_ThenDeserialize_GenericDocument_PreservesExactTypeAndFiles() {
        var original = DeserializeArchive(GenericFiles());

        var result = RoundTrip(original);

        var exact = Assert.IsType<ThreeMFDocument>(result);
        Assert.Equal(ModelPartPath, exact.Core.PartPath);
        Assert.Single(exact.Core.MainModel.Build.Items);
        AssertFilesEqual(original, exact);
    }

    [Fact]
    public void Serialize_ThenDeserialize_BambuDocument_PreservesSemanticsAndType() {
        var original = DeserializeArchive(BambuProjectFiles());

        var result = RoundTrip(original);

        var bambu = Assert.IsType<BambuThreeMFDocument>(result);
        Assert.Equal("BambuStudio 01.09.05.51", bambu.Project.Application);
        Assert.Equal("bambu-studio", bambu.Project.Header.ClientType);
        Assert.Equal("01.09.05.51", bambu.Project.Header.ClientVersion);
        Assert.Equal("0.2", bambu.Project.ProjectSettings.Values["layer_height"]);
        Assert.Equal(2, bambu.Project.ModelSettings.PlateConfigs.Count);
        Assert.Equal(new[] { 1, 2 }, bambu.Plates.Select(plate => plate.Index));
        Assert.Equal("Default Plate", bambu.Plates[0].Name);
        Assert.NotNull(bambu.Plates[0].Thumbnail);
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], bambu.Plates[0].Thumbnail!.Data.ToArray());
        AssertFilesEqual(original, bambu);
    }

    [Fact]
    public void Serialize_ThenDeserialize_GCodeDocument_PreservesSemanticsAndType() {
        var files = BambuProjectFiles();
        files["Metadata/plate_1.gcode"] = Text("; sliced gcode");
        var original = DeserializeArchive(files);

        var result = RoundTrip(original);

        var gcode = Assert.IsType<BambuGCodeThreeMFDocument>(result);
        Assert.Equal(["Metadata/plate_1.gcode"], gcode.PrintJob.GCodeParts);
        Assert.Equal("Metadata/plate_1.gcode", gcode.Plates[0].GCodePart);
        AssertFilesEqual(original, gcode);
    }

    [Fact]
    public void Serialize_UnknownBinaryFile_PreservesBytesExactly() {
        var files = BambuProjectFiles();
        files["Assets/arbitrary.bin"] = Enumerable.Range(0, 512)
            .Select(i => (byte) (i * 31 + 7))
            .ToArray();
        var original = DeserializeArchive(files);

        var result = RoundTrip(original);

        Assert.True(result.Files.TryGetValue("Assets/arbitrary.bin", out var bytes));
        Assert.Equal(files["Assets/arbitrary.bin"], bytes.ToArray());
        AssertFilesEqual(original, result);
    }

    [Fact]
    public void Serialize_EmptyFiles_ArePreserved() {
        var files = GenericFiles();
        files["Metadata/empty.bin"] = [];
        files["3D/nested/empty.txt"] = [];
        var original = DeserializeArchive(files);

        var result = RoundTrip(original);

        Assert.True(result.Files.TryGetValue("Metadata/empty.bin", out var emptyBin));
        Assert.Empty(emptyBin.ToArray());

        Assert.True(result.Files.TryGetValue("3D/nested/empty.txt", out var emptyText));
        Assert.Empty(emptyText.ToArray());

        AssertFilesEqual(original, result);
    }

    [Fact]
    public void Serialize_PackageMetadata_IsPreserved() {
        var original = DeserializeArchive(GenericFiles());

        var result = RoundTrip(original);

        Assert.Equal(original.Package.ContentTypes, result.Package.ContentTypes);
        Assert.Equal(original.Package.Relationships, result.Package.Relationships);
    }

    [Fact]
    public void Serialize_BaseTypeReferences_DispatchByRuntimeType() {
        var files = BambuProjectFiles();
        files["Metadata/plate_1.gcode"] = Text("; sliced gcode");

        var asCore = DeserializeArchive(files);
        var asBambu = DeserializeArchive(CloneFiles(files));

        var coreTyped = (ThreeMFDocument) asCore;
        var bambuTyped = (BambuThreeMFDocument) asBambu;

        Assert.IsType<BambuGCodeThreeMFDocument>(RoundTrip(coreTyped));
        Assert.IsType<BambuGCodeThreeMFDocument>(RoundTrip(bambuTyped));
    }

    [Fact]
    public void Serialize_LeavesStreamOpen() {
        var original = DeserializeArchive(GenericFiles());

        using var stream = new MemoryStream();

        ThreeMFSerializer.Serialize(stream, original);

        Assert.True(stream.CanRead);
        Assert.True(stream.CanWrite);
        Assert.True(stream.CanSeek);
    }

    [Fact]
    public void Serialize_NullOptions_UsesDefaults() {
        var original = DeserializeArchive(BambuProjectFiles());

        using var omittedStream = new MemoryStream();
        ThreeMFSerializer.Serialize(omittedStream, original);

        using var explicitNullStream = new MemoryStream();
        ThreeMFSerializer.Serialize(explicitNullStream, original, null);

        omittedStream.Position = 0;
        Assert.IsType<BambuThreeMFDocument>(ThreeMFSerializer.Deserialize(omittedStream));

        explicitNullStream.Position = 0;
        Assert.IsType<BambuThreeMFDocument>(ThreeMFSerializer.Deserialize(explicitNullStream));
    }

    [Fact]
    public void Serialize_NullArguments_Throw() {
        var document = DeserializeArchive(GenericFiles());

        using var stream = new MemoryStream();

        Assert.Throws<ArgumentNullException>(() => ThreeMFSerializer.Serialize(null!, document));
        Assert.Throws<ArgumentNullException>(() => ThreeMFSerializer.Serialize(stream, null!));
    }

    [Fact]
    public void Select_PrefersMostSpecificWriterForRuntimeType() {
        Assert.Equal(
            typeof(BambuGCodeThreeMFDocument),
            DocumentWriterRegistry.Select(typeof(BambuGCodeThreeMFDocument), []).DocumentType
        );
        Assert.Equal(
            typeof(BambuThreeMFDocument),
            DocumentWriterRegistry.Select(typeof(BambuThreeMFDocument), []).DocumentType
        );
        Assert.Equal(
            typeof(ThreeMFDocument),
            DocumentWriterRegistry.Select(typeof(ThreeMFDocument), []).DocumentType
        );
    }

    [Fact]
    public void Select_UnregisteredDerivedTypes_FallBackToNearestWriter() {
        Assert.Equal(
            typeof(BambuThreeMFDocument),
            DocumentWriterRegistry.Select(typeof(VendorBambuDocument), []).DocumentType
        );
        Assert.Equal(
            typeof(ThreeMFDocument),
            DocumentWriterRegistry.Select(typeof(VendorCoreDocument), []).DocumentType
        );
    }

    static ThreeMFDocument DeserializeArchive(IReadOnlyDictionary<string, byte[]> files) {
        using var stream = CreateArchive(files);

        return ThreeMFSerializer.Deserialize(stream);
    }

    static ThreeMFDocument RoundTrip(ThreeMFDocument document) {
        using var stream = new MemoryStream();

        ThreeMFSerializer.Serialize(stream, document);

        stream.Position = 0;

        return ThreeMFSerializer.Deserialize(stream);
    }

    static void AssertFilesEqual(ThreeMFDocument expected, ThreeMFDocument actual) {
        Assert.Equal(expected.Files.Count, actual.Files.Count);

        foreach (var (path, bytes) in expected.Files) {
            Assert.True(actual.Files.TryGetValue(path, out var roundTripped));
            Assert.Equal(bytes.ToArray(), roundTripped.ToArray());
        }
    }

    static Dictionary<string, byte[]> GenericFiles() {
        return new Dictionary<string, byte[]> {
            ["[Content_Types].xml"] = Text(ContentTypesPart),
            ["_rels/.rels"] = Text(RelationshipsPart),
            [ModelPartPath] = Text(GenericModel),
            ["Metadata/thumbnail.png"] = [0x89, 0x50, 0x4E, 0x47],
        };
    }

    static Dictionary<string, byte[]> BambuProjectFiles() {
        return new Dictionary<string, byte[]> {
            ["[Content_Types].xml"] = Text(ContentTypesPart),
            ["_rels/.rels"] = Text(RelationshipsPart),
            [ModelPartPath] = Text(BambuModel),
            ["Metadata/header_item"] = Text(HeaderItemPart),
            ["Metadata/project_settings.config"] = Text(ProjectSettingsPart),
            ["Metadata/model_settings.config"] = Text(ModelSettingsPart),
            ["Metadata/plate_1.png"] = [0x89, 0x50, 0x4E, 0x47],
            ["Metadata/plate_1_small.png"] = [0x21],
        };
    }

    static Dictionary<string, byte[]> CloneFiles(IReadOnlyDictionary<string, byte[]> files) {
        return files.ToDictionary(pair => pair.Key, pair => pair.Value);
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

    sealed class VendorBambuDocument : BambuThreeMFDocument { }

    sealed class VendorCoreDocument : ThreeMFDocument { }
}
