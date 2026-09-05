using System.IO.Compression;
using System.Text;
using Shiron.Lib.Tess.ThreeMF.Exceptions;

namespace Shiron.Lib.Tess.ThreeMF.Tests;

public class BambuDocumentTests {
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

    const string GenericModel = """
        <?xml version="1.0" encoding="UTF-8"?>
        <model unit="millimeter" xmlns="http://schemas.microsoft.com/3dmanufacturing/core/2015/02">
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

    const string HeaderItemXmlPart = """
        <?xml version="1.0" encoding="UTF-8"?>
        <header_item key="X-BBL-Client-Type" value="slicer"/>
        """;

    const string HeaderItemConfigXmlPart = """
        <?xml version="1.0" encoding="UTF-8"?>
        <config>
          <header_item key="X-BBL-Client-Type">bambu-studio</header_item>
          <header_item key="X-BBL-Client-Version" value="01.09.05.51"/>
          <header_item key="X-BBL-Extra-Marker" value="marker"/>
        </config>
        """;

    const string GenericHeaderItemXmlPart = """
        <?xml version="1.0" encoding="UTF-8"?>
        <config>
          <header_item key="Client-Type" value="other-slicer"/>
        </config>
        """;

    const string ProjectSettingsPart = """
        {
          "layer_height": "0.2",
          "nozzle_temperature": 220,
          "enable_bed": true,
          "filament_settings": [ "Bambu PLA @BBL X1C", "Bambu PLA @base" ]
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
    public void Deserialize_BambuProject_DetectsExactBambuDocument() {
        using var stream = CreateArchive(BambuProjectFiles());

        var document = ThreeMFSerializer.Deserialize(stream);

        var bambu = Assert.IsType<BambuThreeMFDocument>(document);
        Assert.Equal("BambuStudio 01.09.05.51", bambu.Project.Application);
        Assert.Equal(ModelPartPath, bambu.Core.PartPath);
        Assert.Contains("Metadata/project_settings.config", bambu.Files.Keys);
        Assert.Contains("Metadata/model_settings.config", bambu.Files.Keys);
        Assert.Contains("Metadata/header_item", bambu.Files.Keys);
    }

    [Fact]
    public void Deserialize_GenericPackage_StillDetectsExactStandardDocument() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["[Content_Types].xml"] = Text(ContentTypesPart),
            ["_rels/.rels"] = Text(RelationshipsPart),
            [ModelPartPath] = Text(GenericModel),
            ["Metadata/thumbnail.png"] = [0x89, 0x50, 0x4E, 0x47],
        });

        var document = ThreeMFSerializer.Deserialize(stream);

        Assert.IsType<ThreeMFDocument>(document);
    }

    [Fact]
    public void Deserialize_SlicedGCode_DetectsExactGCodeDocument() {
        var files = BambuProjectFiles();
        files["Metadata/plate_1.gcode"] = Text("; sliced gcode");
        using var stream = CreateArchive(files);

        var document = ThreeMFSerializer.Deserialize(stream);

        var gcode = Assert.IsType<BambuGCodeThreeMFDocument>(document);
        Assert.Equal(new[] { "Metadata/plate_1.gcode" }, gcode.PrintJob.GCodeParts);
        Assert.Equal("Metadata/plate_1.gcode", gcode.Plates[0].GCodePart);
    }

    [Fact]
    public void Deserialize_BaseTypeRequests_PreserveRuntimeTypes() {
        var gcodeFiles = BambuProjectFiles();
        gcodeFiles["Metadata/plate_1.gcode"] = Text("; sliced gcode");

        using var gcodeStream = CreateArchive(gcodeFiles);
        Assert.IsType<BambuGCodeThreeMFDocument>(
            ThreeMFSerializer.Deserialize<BambuThreeMFDocument>(gcodeStream)
        );

        using var bambuStream = CreateArchive(BambuProjectFiles());
        Assert.IsType<BambuThreeMFDocument>(
            ThreeMFSerializer.Deserialize<ThreeMFDocument>(bambuStream)
        );
    }

    [Fact]
    public void Deserialize_RequestedBambuOnGenericPackage_ThrowsMismatch() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["_rels/.rels"] = Text(RelationshipsPart),
            [ModelPartPath] = Text(GenericModel),
        });

        var exception = Assert.Throws<ThreeMFDocumentTypeMismatchException>(
            () => ThreeMFSerializer.Deserialize<BambuThreeMFDocument>(stream)
        );

        Assert.Equal(typeof(BambuThreeMFDocument), exception.RequestedType);
        Assert.Equal(typeof(ThreeMFDocument), exception.ActualType);
    }

    [Fact]
    public void Deserialize_RequestedGCodeOnProjectPackage_ThrowsMismatch() {
        using var stream = CreateArchive(BambuProjectFiles());

        var exception = Assert.Throws<ThreeMFDocumentTypeMismatchException>(
            () => ThreeMFSerializer.Deserialize<BambuGCodeThreeMFDocument>(stream)
        );

        Assert.Equal(typeof(BambuGCodeThreeMFDocument), exception.RequestedType);
        Assert.Equal(typeof(BambuThreeMFDocument), exception.ActualType);
    }

    [Fact]
    public void Deserialize_HeaderAndProjectSettings_PreserveRawValues() {
        using var stream = CreateArchive(BambuProjectFiles());

        var document = ThreeMFSerializer.Deserialize(stream);

        var project = Assert.IsType<BambuThreeMFDocument>(document).Project;

        Assert.Equal("bambu-studio", project.Header.ClientType);
        Assert.Equal("01.09.05.51", project.Header.ClientVersion);
        Assert.Equal(2, project.Header.Items.Count);

        Assert.Equal("0.2", project.ProjectSettings.Values["layer_height"]);
        Assert.Equal("220", project.ProjectSettings.Values["nozzle_temperature"]);
        Assert.Equal("true", project.ProjectSettings.Values["enable_bed"]);
        Assert.Contains("Bambu PLA @BBL X1C", project.ProjectSettings.Values["filament_settings"]);

        Assert.Equal(2, project.ModelSettings.PlateConfigs.Count);
        Assert.Equal("X1C", project.ModelSettings.PlateConfigs[1]["printer_model_id"]);
        Assert.Equal("Default Plate", project.ModelSettings.PlateConfigs[1]["name"]);
    }

    [Fact]
    public void Deserialize_HeaderClientMarkersOnly_StillDetectsBambu() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["_rels/.rels"] = Text(RelationshipsPart),
            [ModelPartPath] = Text(GenericModel),
            ["Metadata/header_item"] = Text(HeaderItemPart),
        });

        var document = ThreeMFSerializer.Deserialize(stream);

        var bambu = Assert.IsType<BambuThreeMFDocument>(document);
        Assert.Equal("bambu-studio", bambu.Project.Header.ClientType);
        Assert.Empty(bambu.Project.ProjectSettings.Values);
        Assert.Empty(bambu.Plates);
    }

    [Fact]
    public void Deserialize_HeaderItemXmlForm_MarkerOnlyDetectsBambu() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["_rels/.rels"] = Text(RelationshipsPart),
            [ModelPartPath] = Text(GenericModel),
            ["Metadata/header_item"] = Text(HeaderItemXmlPart),
        });

        var document = ThreeMFSerializer.Deserialize(stream);

        var bambu = Assert.IsType<BambuThreeMFDocument>(document);
        Assert.Equal("slicer", bambu.Project.Header.ClientType);
        Assert.Null(bambu.Project.Header.ClientVersion);
        Assert.Empty(bambu.Project.ProjectSettings.Values);
        Assert.Empty(bambu.Plates);
    }

    [Fact]
    public void Deserialize_HeaderItemXmlInConfigWrapper_MarkerOnlyDetectsBambu() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["_rels/.rels"] = Text(RelationshipsPart),
            [ModelPartPath] = Text(GenericModel),
            ["Metadata/header_item"] = Text(HeaderItemConfigXmlPart),
        });

        var document = ThreeMFSerializer.Deserialize(stream);

        var bambu = Assert.IsType<BambuThreeMFDocument>(document);
        Assert.Equal("bambu-studio", bambu.Project.Header.ClientType);
        Assert.Equal("01.09.05.51", bambu.Project.Header.ClientVersion);
        Assert.Equal(3, bambu.Project.Header.Items.Count);
    }

    [Fact]
    public void Deserialize_HeaderItemXmlWithoutBambuMarkers_RemainsStandardDocument() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["_rels/.rels"] = Text(RelationshipsPart),
            [ModelPartPath] = Text(GenericModel),
            ["Metadata/header_item"] = Text(GenericHeaderItemXmlPart),
        });

        var document = ThreeMFSerializer.Deserialize(stream);

        Assert.IsType<ThreeMFDocument>(document);
    }

    [Fact]
    public void Deserialize_MalformedHeaderItemXml_RemainsStandardDocument() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["_rels/.rels"] = Text(RelationshipsPart),
            [ModelPartPath] = Text(GenericModel),
            ["Metadata/header_item"] = Text("<header_item key=\"X-BBL-Client-Type\""),
        });

        var document = ThreeMFSerializer.Deserialize(stream);

        Assert.IsType<ThreeMFDocument>(document);
    }

    [Fact]
    public void Deserialize_Plates_MergeConfigAndPlateFiles() {
        using var stream = CreateArchive(BambuProjectFiles());

        var document = ThreeMFSerializer.Deserialize(stream);

        var plates = Assert.IsType<BambuThreeMFDocument>(document).Plates;

        Assert.Equal(new[] { 1, 2 }, plates.Select(plate => plate.Index));

        var plate1 = plates[0];
        Assert.Equal("Default Plate", plate1.Name);
        Assert.Equal(new[] { 1, 2 }, plate1.Objects.Select(obj => obj.ObjectId));
        Assert.Null(plate1.GCodePart);
        Assert.Equal("X1C", plate1.Config["printer_model_id"]);

        Assert.NotNull(plate1.Thumbnail);
        Assert.Equal("Metadata/plate_1.png", plate1.Thumbnail!.PartPath);
        Assert.Equal("Metadata/plate_1_small.png", plate1.Thumbnail.SmallPartPath);
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], plate1.Thumbnail.Data.ToArray());
        Assert.Equal([0x21], plate1.Thumbnail.SmallData.ToArray());

        var plate2 = plates[1];
        Assert.Equal("Second Plate", plate2.Name);
        Assert.Null(plate2.Thumbnail);
        Assert.Null(plate2.GCodePart);

        var object3 = Assert.Single(plate2.Objects);
        Assert.Equal(3, object3.ObjectId);
        Assert.Equal("Cube", object3.Name);
    }

    [Fact]
    public void Deserialize_PlateFilesWithoutConfig_AreStillDetected() {
        var files = new Dictionary<string, byte[]> {
            ["_rels/.rels"] = Text(RelationshipsPart),
            [ModelPartPath] = Text(BambuModel),
            ["Metadata/plate_1.png"] = [0x89, 0x50, 0x4E, 0x47],
        };
        using var stream = CreateArchive(files);

        var document = ThreeMFSerializer.Deserialize(stream);

        var plate = Assert.Single(Assert.IsType<BambuThreeMFDocument>(document).Plates);

        Assert.Equal(1, plate.Index);
        Assert.Null(plate.Name);
        Assert.Empty(plate.Objects);
        Assert.Empty(plate.Config);
        Assert.NotNull(plate.Thumbnail);
    }

    [Fact]
    public void Deserialize_MissingMetadataParts_YieldEmptyProjectParts() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["_rels/.rels"] = Text(RelationshipsPart),
            [ModelPartPath] = Text(BambuModel),
        });

        var document = ThreeMFSerializer.Deserialize(stream);

        var project = Assert.IsType<BambuThreeMFDocument>(document).Project;

        Assert.Empty(project.Header.Items);
        Assert.Null(project.Header.ClientType);
        Assert.Null(project.Header.ClientVersion);
        Assert.Empty(project.ProjectSettings.Values);
        Assert.Empty(project.ModelSettings.PlateConfigs);
        Assert.Empty(Assert.IsType<BambuThreeMFDocument>(document).Plates);
    }

    [Fact]
    public void Deserialize_MalformedProjectSettings_ThrowsBambuFormat() {
        var files = new Dictionary<string, byte[]> {
            ["_rels/.rels"] = Text(RelationshipsPart),
            [ModelPartPath] = Text(BambuModel),
            ["Metadata/project_settings.config"] = Text("{ this is not json"),
        };
        using var stream = CreateArchive(files);

        var exception = Assert.Throws<BambuFormatException>(
            () => ThreeMFSerializer.Deserialize(stream)
        );

        Assert.IsAssignableFrom<ThreeMFFormatException>(exception);
        Assert.Contains("project_settings.config", exception.Message);
    }

    [Fact]
    public void Deserialize_InvalidPlateIndex_ThrowsBambuFormat() {
        var files = new Dictionary<string, byte[]> {
            ["_rels/.rels"] = Text(RelationshipsPart),
            [ModelPartPath] = Text(BambuModel),
            ["Metadata/model_settings.config"] = Text(
                """<config><plate index="first"><metadata type="plate_name">Broken</metadata></plate></config>"""
            ),
        };
        using var stream = CreateArchive(files);

        var exception = Assert.Throws<BambuFormatException>(
            () => ThreeMFSerializer.Deserialize(stream)
        );

        Assert.IsAssignableFrom<ThreeMFFormatException>(exception);
        Assert.Contains("plate index", exception.Message);
    }

    [Fact]
    public void Deserialize_ModelSettingsIndexMetadata_HonorsPlateIndex() {
        const string modelSettings = """
            <?xml version="1.0" encoding="UTF-8"?>
            <config>
              <plate>
                <metadata type="index">3</metadata>
                <metadata type="plate_name">Metadata Declared Plate</metadata>
                <metadata type="objects">7 8</metadata>
                <metadata type="printer_model_id">X1C</metadata>
              </plate>
            </config>
            """;
        var files = new Dictionary<string, byte[]> {
            ["_rels/.rels"] = Text(RelationshipsPart),
            [ModelPartPath] = Text(BambuModel),
            ["Metadata/model_settings.config"] = Text(modelSettings),
        };
        using var stream = CreateArchive(files);

        var bambu = Assert.IsType<BambuThreeMFDocument>(ThreeMFSerializer.Deserialize(stream));

        var plate = Assert.Single(bambu.Plates);
        Assert.Equal(3, plate.Index);
        Assert.Equal("Metadata Declared Plate", plate.Name);
        Assert.Equal(new[] { 7, 8 }, plate.Objects.Select(obj => obj.ObjectId));
        Assert.Equal("X1C", plate.Config["printer_model_id"]);
        Assert.Equal(3, bambu.Project.ModelSettings.PlateConfigs.Keys.Single());
    }

    [Fact]
    public void Deserialize_InvalidPlateIndexMetadata_ThrowsBambuFormat() {
        var files = new Dictionary<string, byte[]> {
            ["_rels/.rels"] = Text(RelationshipsPart),
            [ModelPartPath] = Text(BambuModel),
            ["Metadata/model_settings.config"] = Text(
                """<config><plate><metadata type="index">first</metadata></plate></config>"""
            ),
        };
        using var stream = CreateArchive(files);

        var exception = Assert.Throws<BambuFormatException>(
            () => ThreeMFSerializer.Deserialize(stream)
        );

        Assert.IsAssignableFrom<ThreeMFFormatException>(exception);
        Assert.Contains("plate index", exception.Message);
    }

    [Fact]
    public void Deserialize_BambuFilesWithoutModelPart_ThrowDetectionException() {
        using var stream = CreateArchive(new Dictionary<string, byte[]> {
            ["Metadata/project_settings.config"] = Text(ProjectSettingsPart),
            ["Metadata/plate_1.png"] = [0x89, 0x50, 0x4E, 0x47],
        });

        Assert.Throws<ThreeMFDocumentDetectionException>(
            () => ThreeMFSerializer.Deserialize(stream)
        );
    }

    [Fact]
    public void Deserialize_PreserveUnknownFilesFalse_KeepsRecognizedBambuParts() {
        var files = BambuProjectFiles();
        files["Metadata/plate_1.gcode"] = Text("; sliced gcode");
        files["Metadata/notes.txt"] = Text("unrelated");
        using var stream = CreateArchive(files);

        var options = ThreeMFSerializerOptions.Default with { PreserveUnknownFiles = false };
        var document = ThreeMFSerializer.Deserialize(stream, options);

        Assert.Contains(ModelPartPath, document.Files.Keys);
        Assert.Contains("Metadata/header_item", document.Files.Keys);
        Assert.Contains("Metadata/project_settings.config", document.Files.Keys);
        Assert.Contains("Metadata/model_settings.config", document.Files.Keys);
        Assert.Contains("Metadata/plate_1.png", document.Files.Keys);
        Assert.Contains("Metadata/plate_1.gcode", document.Files.Keys);
        Assert.DoesNotContain("Metadata/notes.txt", document.Files.Keys);
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
