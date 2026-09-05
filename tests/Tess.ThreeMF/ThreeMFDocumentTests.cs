using System.Diagnostics.CodeAnalysis;

namespace Shiron.Lib.Tess.ThreeMF.Tests;

public class ThreeMFDocumentTests {
    const string ModelPartPath = "3D/3dmodel.model";

    sealed class DerivedDocument : ThreeMFDocument {
        public int Revision { get; set; }

        [SetsRequiredMembers]
        public DerivedDocument(ThreeMFPackage package, Core core) {
            Files = package.Files;
            Package = package;
            Core = core;
        }
    }

    [Fact]
    public void Constructor_DerivedDocument_InheritsAndWiresRequiredMembers() {
        var (package, core) = CreateParts();

        ThreeMFDocument document = new DerivedDocument(package, core);

        var derived = Assert.IsType<DerivedDocument>(document);
        Assert.Same(package, derived.Package);
        Assert.Same(core, derived.Core);
        Assert.Same(package.Files, derived.Files);
        Assert.Equal(0, derived.Revision);
    }

    [Fact]
    public void Core_ModelsAndMainModel_ExposeParsedShape() {
        var (_, core) = CreateParts();

        var model = Assert.Single(core.Models);
        Assert.Same(model, core.MainModel);
        Assert.Equal(ModelPartPath, core.PartPath);
    }

    static (ThreeMFPackage Package, Core Core) CreateParts() {
        var model = new Model {
            Resources = new Resources { Objects = [] },
        };

        var package = new ThreeMFPackage {
            Files = new Dictionary<string, ReadOnlyMemory<byte>> {
                [ModelPartPath] = ReadOnlyMemory<byte>.Empty,
            },
        };

        var core = new Core {
            PartPath = ModelPartPath,
            Models = [model],
            MainModel = model,
        };

        return (package, core);
    }
}
