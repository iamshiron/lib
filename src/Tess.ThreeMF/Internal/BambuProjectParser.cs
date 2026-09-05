using System.Globalization;
using System.Text.Json;
using System.Xml;
using Shiron.Lib.Tess.ThreeMF.Exceptions;

namespace Shiron.Lib.Tess.ThreeMF.Internal;

/// <summary>
/// Parses the producer-specific parts of a Bambu 3MF package on top of one shared OPC
/// package and one 3MF core parse, constructing the derived Bambu documents directly
/// instead of creating or upgrading a base document.
/// </summary>
internal static class BambuProjectParser {
    static readonly XmlReaderSettings Settings = new() {
        DtdProcessing = DtdProcessing.Ignore,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreWhitespace = true,
        CloseInput = true,
    };

    static readonly IReadOnlyDictionary<string, string> EmptyConfig =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Parses the context into exactly a <see cref="BambuThreeMFDocument"/>.
    /// </summary>
    public static BambuThreeMFDocument ParseDocument(ThreeMFParseContext context) {
        ArgumentNullException.ThrowIfNull(context);

        var (package, core, project, plates) = ParseParts(context);

        return new BambuThreeMFDocument {
            Files = package.Files,
            Package = package,
            Core = core,
            Project = project,
            Plates = plates,
        };
    }

    /// <summary>
    /// Parses the context into exactly a <see cref="BambuGCodeThreeMFDocument"/>,
    /// summarizing the embedded sliced plate G-code as a print job.
    /// </summary>
    public static BambuGCodeThreeMFDocument ParseGCodeDocument(ThreeMFParseContext context) {
        ArgumentNullException.ThrowIfNull(context);

        var (package, core, project, plates) = ParseParts(context);

        return new BambuGCodeThreeMFDocument {
            Files = package.Files,
            Package = package,
            Core = core,
            Project = project,
            Plates = plates,
            PrintJob = new BambuPrintJob {
                GCodeParts = [.. plates.Select(plate => plate.GCodePart).OfType<string>()],
            },
        };
    }

    static (ThreeMFPackage Package, Core Core, BambuProject Project, IReadOnlyList<BambuPlate> Plates)
        ParseParts(ThreeMFParseContext context) {
        var package = StandardDocumentHandler.ParsePackage(context.Files);
        var core = CoreParser.Parse(package, context.Options.ValidationMode);

        var plateConfigs = ParseModelSettings(context.Files);
        var project = new BambuProject {
            Application = core.MainModel.Metadata.FirstOrDefault(m => m.Name == "Application")?.Value,
            Header = ParseHeader(context.Files),
            ProjectSettings = ParseProjectSettings(context.Files),
            ModelSettings = new BambuModelSettings {
                PlateConfigs = plateConfigs.ToDictionary(config => config.Index, config => config.Raw),
            },
        };
        var plates = BuildPlates(context.Files, plateConfigs);

        if (!context.Options.PreserveUnknownFiles)
            package = RestrictToKnownParts(package, core);

        return (package, core, project, plates);
    }

    static BambuHeader ParseHeader(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) {
        return new BambuHeader {
            Items = files.TryGetValue(BambuParts.HeaderItemPart, out var bytes)
                ? BambuParts.ParseHeaderItems(BambuParts.ReadText(bytes))
                : EmptyConfig,
        };
    }

    static BambuProjectSettings ParseProjectSettings(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) {
        if (!files.TryGetValue(BambuParts.ProjectSettingsPart, out var bytes))
            return new BambuProjectSettings { Values = EmptyConfig };

        try {
            using var document = JsonDocument.Parse(BambuParts.ReadText(bytes));
            var root = document.RootElement;

            if (root.ValueKind is not JsonValueKind.Object)
                throw new BambuFormatException(
                    $"'{BambuParts.ProjectSettingsPart}' must contain a JSON object at the root."
                );

            var values = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var property in root.EnumerateObject()) {
                if (property.Value.ValueKind is JsonValueKind.Null)
                    continue;

                values[property.Name] = property.Value.ValueKind switch {
                    JsonValueKind.String => property.Value.GetString(),
                    _ => property.Value.GetRawText(),
                } ?? string.Empty;
            }

            return new BambuProjectSettings { Values = values };
        } catch (JsonException e) {
            throw new BambuFormatException(
                $"'{BambuParts.ProjectSettingsPart}' is not well-formed JSON.", e
            );
        }
    }

    static IReadOnlyList<BambuPlateConfig> ParseModelSettings(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) {
        if (!files.TryGetValue(BambuParts.ModelSettingsPart, out var bytes))
            return [];

        try {
            using var stream = new MemoryStream(bytes.ToArray(), writable: false);
            using var reader = XmlReader.Create(stream, Settings);

            reader.MoveToContent();

            if (reader.NodeType is not XmlNodeType.Element || reader.LocalName != "config")
                throw new BambuFormatException(
                    $"'{BambuParts.ModelSettingsPart}' must have a <config> root element."
                );

            return ReadPlates(reader);
        } catch (XmlException e) {
            throw new BambuFormatException(
                $"'{BambuParts.ModelSettingsPart}' is not well-formed XML.", e
            );
        }
    }

    static IReadOnlyList<BambuPlateConfig> ReadPlates(XmlReader reader) {
        var plates = new List<BambuPlateConfig>();
        var ordinal = 0;

        if (reader.IsEmptyElement) {
            reader.Skip();
            return plates;
        }

        reader.Read();

        while (reader.NodeType is not (XmlNodeType.EndElement or XmlNodeType.None)) {
            if (reader.NodeType is not XmlNodeType.Element) {
                reader.Read();
                continue;
            }

            if (reader.LocalName == "plate")
                plates.Add(ReadPlate(reader, ++ordinal));
            else
                reader.Skip();
        }

        if (reader.NodeType is XmlNodeType.EndElement)
            reader.Read();

        return plates;
    }

    static BambuPlateConfig ReadPlate(XmlReader reader, int ordinal) {
        var raw = new Dictionary<string, string>(StringComparer.Ordinal);

        while (reader.MoveToNextAttribute())
            raw[reader.LocalName] = reader.Value;

        reader.MoveToElement();

        var name = raw.GetValueOrDefault("name");
        var objects = ParsePlateObjects(raw.GetValueOrDefault("objects"));

        if (!reader.IsEmptyElement) {
            reader.Read();

            while (reader.NodeType is not (XmlNodeType.EndElement or XmlNodeType.None)) {
                if (reader.NodeType is not XmlNodeType.Element) {
                    reader.Read();
                    continue;
                }

                if (reader.LocalName == "metadata") {
                    var type = reader.GetAttribute("type");
                    var value = reader.ReadElementContentAsString();

                    if (string.IsNullOrEmpty(type))
                        continue;

                    raw[type] = value;

                    if (type is "name" or "plate_name")
                        name = value;
                    else if (type is "objects")
                        objects = [.. objects, .. ParsePlateObjects(value)];
                } else if (reader.LocalName == "object") {
                    objects.Add(ReadPlateObject(reader));
                    reader.Skip();
                } else {
                    reader.Skip();
                }
            }

            if (reader.NodeType is XmlNodeType.EndElement)
                reader.Read();
        }

        // Resolve the index last: real packages declare it as a <metadata type="index">
        // child rather than an attribute, and the child loop only merges it into raw.
        var index = raw.TryGetValue("index", out var indexText) ? ParsePlateIndex(indexText) : ordinal;

        return new BambuPlateConfig(index, name, objects, raw);
    }

    static BambuPlateObject ReadPlateObject(XmlReader reader) {
        var idText = reader.GetAttribute("id");

        if (string.IsNullOrWhiteSpace(idText) || !int.TryParse(idText, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var id)) {
            throw new BambuFormatException(
                "A plate <object> declaration is missing a valid 'id' attribute."
            );
        }

        return new BambuPlateObject { ObjectId = id, Name = reader.GetAttribute("name") };
    }

    static List<BambuPlateObject> ParsePlateObjects(string? value) {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        var objects = new List<BambuPlateObject>();

        foreach (var token in value.Split(',', ' ', '\t').Where(token => token.Length > 0)) {
            if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                throw new BambuFormatException($"'{token}' is not a valid plate object id.");

            objects.Add(new BambuPlateObject { ObjectId = id });
        }

        return objects;
    }

    static int ParsePlateIndex(string value) {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) || index < 0)
            throw new BambuFormatException($"'{value}' is not a valid plate index.");

        return index;
    }

    static IReadOnlyList<BambuPlate> BuildPlates(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files,
        IReadOnlyList<BambuPlateConfig> plateConfigs
    ) {
        var indices = new SortedSet<int>();
        var configsByIndex = new Dictionary<int, BambuPlateConfig>();

        foreach (var config in plateConfigs) {
            configsByIndex[config.Index] = config;
            indices.Add(config.Index);
        }

        var imagePaths = new Dictionary<int, string>();
        var smallImagePaths = new Dictionary<int, string>();
        var gcodePaths = new Dictionary<int, string>();

        foreach (var path in files.Keys) {
            if (BambuParts.TryMatchPlateImage(path, out var index)) {
                imagePaths[index] = path;
                indices.Add(index);
            } else if (BambuParts.TryMatchPlateSmallImage(path, out index)) {
                smallImagePaths[index] = path;
                indices.Add(index);
            } else if (BambuParts.TryMatchPlateGCode(path, out index)) {
                gcodePaths[index] = path;
                indices.Add(index);
            }
        }

        var plates = new List<BambuPlate>();

        foreach (var index in indices) {
            var config = configsByIndex.GetValueOrDefault(index);

            plates.Add(new BambuPlate {
                Index = index,
                Name = config?.Name,
                Objects = config?.Objects ?? [],
                Thumbnail = BuildThumbnail(files, index, imagePaths, smallImagePaths),
                GCodePart = gcodePaths.GetValueOrDefault(index),
                Config = config?.Raw ?? EmptyConfig,
            });
        }

        return plates;
    }

    static BambuPlateThumbnail? BuildThumbnail(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files,
        int index,
        IReadOnlyDictionary<int, string> imagePaths,
        IReadOnlyDictionary<int, string> smallImagePaths
    ) {
        imagePaths.TryGetValue(index, out var imagePath);
        smallImagePaths.TryGetValue(index, out var smallImagePath);

        if (imagePath is null && smallImagePath is null)
            return null;

        return new BambuPlateThumbnail {
            PartPath = imagePath ?? smallImagePath!,
            SmallPartPath = smallImagePath,
            Data = imagePath is null ? default : files.GetValueOrDefault(imagePath),
            SmallData = smallImagePath is null ? default : files.GetValueOrDefault(smallImagePath),
        };
    }

    static ThreeMFPackage RestrictToKnownParts(ThreeMFPackage package, Core core) {
        var known = package.Files
            .Where(file => IsKnownPart(file.Key, core.PartPath))
            .ToDictionary(file => file.Key, file => file.Value, StringComparer.Ordinal);

        return new ThreeMFPackage {
            Files = known,
            ContentTypes = package.ContentTypes,
            Relationships = package.Relationships,
        };
    }

    static bool IsKnownPart(string path, string modelPartPath) {
        return path is ThreeMFSerializer.ContentTypesPath or ThreeMFSerializer.RelationshipsPath
            || path == modelPartPath
            || BambuParts.IsRecognizedPart(path);
    }
}
