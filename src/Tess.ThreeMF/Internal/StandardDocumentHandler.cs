namespace Shiron.Lib.Tess.ThreeMF.Internal;

/// <summary>
/// The built-in handler for standard 3MF documents: parses the OPC package view and
/// the 3MF core model into exactly one <see cref="ThreeMFDocument"/>. It claims any
/// package that declares a 3D model part through a relationship or a content type.
/// </summary>
internal sealed class StandardDocumentHandler : IThreeMFDocumentHandler {
    public Type DocumentType => typeof(ThreeMFDocument);

    public ThreeMFProbeResult Probe(ThreeMFParseContext context) {
        ArgumentNullException.ThrowIfNull(context);

        return DeclaresModelPart(context.Files)
            ? ThreeMFProbeResult.Authoritative
            : ThreeMFProbeResult.NoMatch;
    }

    public ThreeMFDocument Parse(ThreeMFParseContext context) {
        ArgumentNullException.ThrowIfNull(context);

        var package = ParsePackage(context.Files);
        var core = CoreParser.Parse(package, context.Options.ValidationMode);

        if (!context.Options.PreserveUnknownFiles)
            package = RestrictToKnownParts(package, core);

        return new ThreeMFDocument {
            Files = package.Files,
            Package = package,
            Core = core,
        };
    }

    static ThreeMFPackage ParsePackage(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) {
        return new ThreeMFPackage {
            Files = files,
            ContentTypes = files.TryGetValue(ThreeMFSerializer.ContentTypesPath, out var contentTypesXml)
                ? OpcParser.ParseContentTypes(contentTypesXml)
                : [],
            Relationships = files.TryGetValue(ThreeMFSerializer.RelationshipsPath, out var relationshipsXml)
                ? OpcParser.ParseRelationships(relationshipsXml)
                : [],
        };
    }

    static ThreeMFPackage RestrictToKnownParts(ThreeMFPackage package, Core core) {
        var known = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);

        if (package.TryGet(ThreeMFSerializer.ContentTypesPath, out var contentTypesXml))
            known[ThreeMFSerializer.ContentTypesPath] = contentTypesXml;

        if (package.TryGet(ThreeMFSerializer.RelationshipsPath, out var relationshipsXml))
            known[ThreeMFSerializer.RelationshipsPath] = relationshipsXml;

        if (package.TryGet(core.PartPath, out var modelXml))
            known[core.PartPath] = modelXml;

        return new ThreeMFPackage {
            Files = known,
            ContentTypes = package.ContentTypes,
            Relationships = package.Relationships,
        };
    }

    static bool DeclaresModelPart(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) {
        return DeclaresModelRelationship(files) || DeclaresModelContentType(files);
    }

    static bool DeclaresModelRelationship(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) {
        if (!files.TryGetValue(ThreeMFSerializer.RelationshipsPath, out var xml))
            return false;

        try {
            return OpcParser.ParseRelationships(xml)
                .Any(r => r.Type == CoreParser.ModelRelationshipType);
        } catch (ThreeMFPackageException) {
            return false;
        }
    }

    static bool DeclaresModelContentType(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) {
        if (!files.TryGetValue(ThreeMFSerializer.ContentTypesPath, out var xml))
            return false;

        try {
            return OpcParser.ParseContentTypes(xml)
                .Any(ct => ct.IsOverride && ct.MediaType == CoreParser.ModelContentType);
        } catch (ThreeMFPackageException) {
            return false;
        }
    }
}
