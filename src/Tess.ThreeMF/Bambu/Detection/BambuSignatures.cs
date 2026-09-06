using System.Xml;
using Shiron.Lib.Tess.ThreeMF.Bambu.Parsing;
using Shiron.Lib.Tess.ThreeMF.Exceptions;
using Shiron.Lib.Tess.ThreeMF.Opc;
using Shiron.Lib.Tess.ThreeMF.Parsing;

namespace Shiron.Lib.Tess.ThreeMF.Bambu.Detection;

/// <summary>
/// Detects explicit Bambu producer signatures in the raw files of a package. Generic
/// 3MF packages without such signatures are never classified as Bambu documents.
/// </summary>
internal static class BambuSignatures {
    public const string ClientMarkerPrefix = "X-BBL-";

    static readonly XmlReaderSettings SafeXml = new() {
        DtdProcessing = DtdProcessing.Ignore,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreWhitespace = true,
        CloseInput = true,
    };

    /// <summary>
    /// Determines whether the files carry any explicit Bambu producer signature: the
    /// Bambu <c>Metadata/</c> config parts, an <c>X-BBL-*</c> client marker in the
    /// <c>Metadata/header_item</c> part or the <c>&lt;header&gt;</c> section of
    /// <c>Metadata/slice_info.config</c>, or Bambu/BBL markers in the 3D model
    /// metadata.
    /// </summary>
    public static bool HasProducerSignature(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) {
        ArgumentNullException.ThrowIfNull(files);

        return files.ContainsKey(BambuParts.ProjectSettingsPart)
            || files.ContainsKey(BambuParts.ModelSettingsPart)
            || HasClientMarkerHeader(files)
            || HasClientMarkerSliceInfo(files)
            || HasProducerModelMetadata(files);
    }

    /// <summary>
    /// Determines whether the files embed a sliced plate G-code part, the marker of a
    /// sliced Bambu print job.
    /// </summary>
    public static bool HasSlicedGCode(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) {
        ArgumentNullException.ThrowIfNull(files);

        return files.Keys.Any(BambuParts.IsPlateGCodePath);
    }

    /// <summary>
    /// Determines whether a header item key is a Bambu client marker, i.e. starts
    /// with <c>X-BBL-</c>.
    /// </summary>
    public static bool IsClientMarkerKey(string key) {
        return key.StartsWith(ClientMarkerPrefix, StringComparison.OrdinalIgnoreCase);
    }

    static bool HasClientMarkerHeader(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) {
        return files.TryGetValue(BambuParts.HeaderItemPart, out var bytes)
            && BambuParts.ParseHeaderItems(BambuParts.ReadText(bytes)).Keys.Any(IsClientMarkerKey);
    }

    /// <summary>
    /// Determines whether <c>Metadata/slice_info.config</c> declares an
    /// <c>X-BBL-*</c> client marker. Conservative: a slice info part without Bambu
    /// client markers never classifies the package as Bambu, and malformed XML yields
    /// no signature because probing must never throw.
    /// </summary>
    static bool HasClientMarkerSliceInfo(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) {
        return files.TryGetValue(BambuParts.SliceInfoPart, out var bytes)
            && BambuParts.ParseHeaderItems(BambuParts.ReadText(bytes)).Keys.Any(IsClientMarkerKey);
    }

    static bool HasProducerModelMetadata(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) {
        var modelPart = LocateModelPart(files);

        return modelPart is not null && ModelMetadataDeclaresProducer(files[modelPart]);
    }

    static bool ModelMetadataDeclaresProducer(ReadOnlyMemory<byte> xml) {
        try {
            using var stream = new MemoryStream(xml.ToArray(), writable: false);
            using var reader = XmlReader.Create(stream, SafeXml);

            reader.MoveToContent();

            if (reader.NodeType is not XmlNodeType.Element || reader.LocalName != "model" || reader.IsEmptyElement)
                return false;

            reader.Read();

            while (reader.NodeType is not (XmlNodeType.EndElement or XmlNodeType.None)) {
                if (reader.NodeType is not XmlNodeType.Element) {
                    reader.Read();
                    continue;
                }

                if (reader.LocalName != "metadata") {
                    reader.Skip();
                    continue;
                }

                if (reader.GetAttribute("name") is { } name && IsProducerName(name))
                    return true;

                if (IsProducerValue(reader.ReadElementContentAsString()))
                    return true;
            }

            return false;
        } catch (XmlException) {
            // Probing must never throw on malformed input.
            return false;
        }
    }

    static bool IsProducerName(string name) {
        return name.StartsWith(ClientMarkerPrefix, StringComparison.OrdinalIgnoreCase)
            || name.Contains("BambuStudio", StringComparison.OrdinalIgnoreCase);
    }

    static bool IsProducerValue(string value) {
        return value.Contains("BambuStudio", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Bambu Studio", StringComparison.OrdinalIgnoreCase);
    }

    static string? LocateModelPart(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) {
        try {
            if (files.TryGetValue(ThreeMFSerializer.RelationshipsPath, out var relationshipsXml)
                && OpcParser.ParseRelationships(relationshipsXml)
                    .FirstOrDefault(r => r.Type == CoreParser.ModelRelationshipType)?.Target
                    is { } target
                && files.ContainsKey(target.TrimStart('/'))) {
                return target.TrimStart('/');
            }

            if (files.TryGetValue(ThreeMFSerializer.ContentTypesPath, out var contentTypesXml))
                return OpcParser.ParseContentTypes(contentTypesXml)
                    .Where(ct => ct.IsOverride && ct.MediaType == CoreParser.ModelContentType)
                    .Select(ct => ct.PartName!.TrimStart('/'))
                    .FirstOrDefault(files.ContainsKey);
        } catch (ThreeMFPackageException) {
            // Malformed OPC metadata cannot locate a model part; probing yields no signature.
        }

        return null;
    }
}
