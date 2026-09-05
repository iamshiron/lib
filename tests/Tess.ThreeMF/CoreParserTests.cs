using System.IO.Compression;
using System.Text;
using Shiron.Lib.Tess.ThreeMF.Internal;

namespace Shiron.Lib.Tess.ThreeMF.Tests;

public class CoreParserTests {
    const string ModelPartPath = "3D/3dmodel.model";

    const string ValidModel = """
        <?xml version="1.0" encoding="UTF-8"?>
        <model unit="centimeter" xml:lang="en-US" xmlns="http://schemas.microsoft.com/3dmanufacturing/core/2015/02">
          <metadata name="Title">Test Part</metadata>
          <resources>
            <object id="1" type="model" name="Quad" partnumber="Q-1">
              <mesh>
                <vertices>
                  <vertex x="0" y="0" z="0" />
                  <vertex x="1" y="0" z="0" />
                  <vertex x="1" y="1" z="0" />
                  <vertex x="0" y="1" z="0" />
                </vertices>
                <triangles>
                  <triangle v1="0" v2="1" v3="2" />
                  <triangle v1="0" v2="2" v3="3" />
                </triangles>
              </mesh>
            </object>
            <object id="2" type="model">
              <components>
                <component objectid="1" transform="2 0 0 0 2 0 0 0 2 1 2 3" />
              </components>
            </object>
          </resources>
          <build>
            <item objectid="2" transform="1 0 0 0 1 0 0 0 1 10 20 30" />
          </build>
        </model>
        """;

    [Fact]
    public void Parse_ValidModel_ParsesMetadataGeometryAndReferences() {
        using var stream = CreatePackage(ValidModel);
        var package = ThreeMFSerializer.DeserializePackage(stream);

        var core = CoreParser.Parse(package);

        Assert.Equal(ModelPartPath, core.PartPath);
        Assert.Same(Assert.Single(core.Models), core.MainModel);
        Assert.Equal(Unit.Centimeter, core.MainModel.Unit);
        Assert.Equal("en-US", core.MainModel.Language);

        var metadata = Assert.Single(core.MainModel.Metadata);
        Assert.Equal("Title", metadata.Name);
        Assert.Equal("Test Part", metadata.Value);

        Assert.Equal(2, core.MainModel.Resources.Objects.Count);

        var meshObject = core.MainModel.Resources.Objects[0];
        Assert.Equal(1, meshObject.Id);
        Assert.Equal(ObjectType.Model, meshObject.Type);
        Assert.Equal("Quad", meshObject.Name);
        Assert.Equal("Q-1", meshObject.PartNumber);
        Assert.Empty(meshObject.Components);

        Assert.NotNull(meshObject.Mesh);

        var mesh = meshObject.Mesh!;
        Assert.Equal(4, mesh.Vertices.Count);
        Assert.Equal(new Vertex(0, 0, 0), mesh.Vertices[0]);
        Assert.Equal(new Vertex(1, 1, 0), mesh.Vertices[2]);
        Assert.Equal(new Vertex(0, 1, 0), mesh.Vertices[3]);

        Assert.Equal(2, mesh.Triangles.Count);
        Assert.Equal(new Triangle(0, 1, 2), mesh.Triangles[0]);
        Assert.Equal(new Triangle(0, 2, 3), mesh.Triangles[1]);

        var compositeObject = core.MainModel.Resources.Objects[1];
        Assert.Equal(2, compositeObject.Id);
        Assert.Null(compositeObject.Mesh);

        var component = Assert.Single(compositeObject.Components);
        Assert.Equal(1, component.ObjectId);
        Assert.Equal(
            new Transform(2, 0, 0, 0, 2, 0, 0, 0, 2, 1, 2, 3),
            component.Transform
        );

        var item = Assert.Single(core.MainModel.Build.Items);
        Assert.Equal(2, item.ObjectId);
        Assert.Equal(
            new Transform(1, 0, 0, 0, 1, 0, 0, 0, 1, 10, 20, 30),
            item.Transform
        );

        Assert.True(core.MainModel.Resources.TryGetObject(2, out var resolved));
        Assert.Same(compositeObject, resolved);
        Assert.False(core.MainModel.Resources.TryGetObject(99, out _));
    }

    [Fact]
    public void Parse_ValidModel_WiresIntoDocument() {
        using var stream = CreatePackage(ValidModel);
        var package = ThreeMFSerializer.DeserializePackage(stream);
        var core = CoreParser.Parse(package);

        var document = new ThreeMFDocument {
            Files = package.Files,
            Package = package,
            Core = core,
        };

        Assert.Same(package, document.Package);
        Assert.Same(core, document.Core);
        Assert.Same(core.MainModel, document.Core.MainModel);
        Assert.Same(package.Files, document.Files);
        Assert.Contains(ModelPartPath, document.Files.Keys);
    }

    [Fact]
    public void Parse_WithoutUnitAttribute_DefaultsToMillimeter() {
        var model = ValidModel.Replace(" unit=\"centimeter\"", "");
        using var stream = CreatePackage(model);

        var package = ThreeMFSerializer.DeserializePackage(stream);

        Assert.Equal(Unit.Millimeter, CoreParser.Parse(package).MainModel.Unit);
    }

    [Fact]
    public void Parse_TriangleReferencingMissingVertex_Throws() {
        var model = ValidModel.Replace(
            "<triangle v1=\"0\" v2=\"1\" v3=\"2\" />",
            "<triangle v1=\"0\" v2=\"1\" v3=\"99\" />"
        );
        using var stream = CreatePackage(model);

        var package = ThreeMFSerializer.DeserializePackage(stream);

        var exception = Assert.Throws<ThreeMFCoreException>(
            () => CoreParser.Parse(package)
        );
        Assert.Contains("vertex 99", exception.Message);
    }

    [Fact]
    public void Parse_BuildItemReferencingUnknownObject_Throws() {
        var model = ValidModel.Replace(
            "<item objectid=\"2\"",
            "<item objectid=\"42\""
        );
        using var stream = CreatePackage(model);

        var package = ThreeMFSerializer.DeserializePackage(stream);

        Assert.Throws<ThreeMFCoreException>(() => CoreParser.Parse(package));
    }

    [Fact]
    public void Parse_ComponentReferencingUnknownObject_Throws() {
        var model = ValidModel.Replace(
            "<component objectid=\"1\"",
            "<component objectid=\"99\""
        );
        using var stream = CreatePackage(model);

        var package = ThreeMFSerializer.DeserializePackage(stream);

        Assert.Throws<ThreeMFCoreException>(() => CoreParser.Parse(package));
    }

    [Fact]
    public void Parse_MalformedModelXml_Throws() {
        var model = "<model xmlns=\"http://schemas.microsoft.com/3dmanufacturing/core/2015/02\"><resources>";
        using var stream = CreatePackage(model);

        var package = ThreeMFSerializer.DeserializePackage(stream);

        Assert.Throws<ThreeMFCoreException>(() => CoreParser.Parse(package));
    }

    [Fact]
    public void Parse_WithoutModelPart_Throws() {
        using var stream = CreatePackage(model: null, includeRelationships: false);

        var package = ThreeMFSerializer.DeserializePackage(stream);

        Assert.Throws<ThreeMFCoreException>(() => CoreParser.Parse(package));
    }

    [Fact]
    public void Parse_WithoutRelationship_SelectsModelViaContentType() {
        using var stream = CreatePackage(ValidModel, includeRelationships: false);

        var package = ThreeMFSerializer.DeserializePackage(stream);

        var core = CoreParser.Parse(package);

        Assert.Empty(package.Relationships);
        Assert.Equal(ModelPartPath, core.PartPath);
        Assert.Same(Assert.Single(core.Models), core.MainModel);
        Assert.Equal(Unit.Centimeter, core.MainModel.Unit);
    }

    static MemoryStream CreatePackage(string? model, bool includeRelationships = true) {
        var files = new Dictionary<string, byte[]> {
            ["[Content_Types].xml"] = """
                <?xml version="1.0" encoding="UTF-8"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml" />
                  <Override PartName="/3D/3dmodel.model" ContentType="application/vnd.ms-package.3dmanufacturing-3dmodel+xml" />
                </Types>
                """u8.ToArray(),
        };

        if (includeRelationships) {
            files["_rels/.rels"] = """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rel-model" Type="http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel" Target="/3D/3dmodel.model" />
                </Relationships>
                """u8.ToArray();
        }

        if (model is not null)
            files[ModelPartPath] = Encoding.UTF8.GetBytes(model);

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
