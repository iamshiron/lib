using System.Globalization;
using System.Text.Json;
using System.Xml;
using Shiron.Lib.Tess.ThreeMF.Detection;
using Shiron.Lib.Tess.ThreeMF.Exceptions;
using Shiron.Lib.Tess.ThreeMF.Parsing;

namespace Shiron.Lib.Tess.ThreeMF.Bambu.Parsing;

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
    /// Parses the context into exactly a <see cref="BambuDocument"/>.
    /// </summary>
    public static BambuDocument ParseDocument(ParseContext context) {
        ArgumentNullException.ThrowIfNull(context);

        var (package, core, project, plates) = ParseParts(context);

        return new BambuDocument {
            Files = package.Files,
            Package = package,
            Core = core,
            Project = project,
            Plates = plates,
        };
    }

    /// <summary>
    /// Parses the context into exactly a <see cref="BambuGCodeDocument"/>,
    /// summarizing the embedded sliced plate G-code as a print job.
    /// </summary>
    public static BambuGCodeDocument ParseGCodeDocument(ParseContext context) {
        ArgumentNullException.ThrowIfNull(context);

        var (package, core, project, plates) = ParseParts(context);

        return new BambuGCodeDocument {
            Files = package.Files,
            Package = package,
            Core = core,
            Project = project,
            Plates = plates,
            PrintJob = new BambuPrintJob {
                GCodeParts = [.. plates.Select(plate => plate.GCodePart).OfType<string>()],
                GCodeByPlate = plates
                    .Where(plate => plate.GCode is not null)
                    .ToDictionary(plate => plate.Index, plate => plate.GCode!),
            },
        };
    }

    static (ThreeMFPackage Package, ThreeMFCore Core, BambuProject Project, IReadOnlyList<BambuPlate> Plates)
        ParseParts(ParseContext context) {
        var package = StandardDocumentHandler.ParsePackage(context.Files);
        var core = CoreParser.Parse(package, context.Options.ValidationMode);

        var modelSettings = ParseModelSettings(context.Files);
        var slicePlates = ParseSlicePlates(context.Files);
        var project = new BambuProject {
            Application = core.MainModel.Metadata.FirstOrDefault(m => m.Name == "Application")?.Value,
            Header = ParseHeader(context.Files),
            ProjectSettings = ParseProjectSettings(context.Files),
            ModelSettings = new BambuModelSettings {
                PlateConfigs = modelSettings.Plates.ToDictionary(config => config.Index, config => config.Raw),
                Objects = modelSettings.Objects,
                AssemblyItems = modelSettings.AssemblyItems,
            },
            SlicePlates = slicePlates,
        };
        var plates = BuildPlates(context.Files, modelSettings.Plates, modelSettings.Objects, slicePlates);

        if (!context.Options.PreserveUnknownFiles)
            package = RestrictToKnownParts(package, core);

        return (package, core, project, plates);
    }

    static BambuHeader ParseHeader(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) {
        if (files.TryGetValue(BambuParts.HeaderItemPart, out var bytes))
            return new BambuHeader { Items = BambuParts.ParseHeaderItems(BambuParts.ReadText(bytes)) };

        // Real packages without a dedicated header part declare the same
        // <header_item> elements inside the <header> section of the slice info part.
        if (files.TryGetValue(BambuParts.SliceInfoPart, out var sliceInfoBytes))
            return new BambuHeader { Items = BambuParts.ParseHeaderItems(BambuParts.ReadText(sliceInfoBytes)) };

        return new BambuHeader { Items = EmptyConfig };
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

    static BambuModelSettingsData ParseModelSettings(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) {
        if (!files.TryGetValue(BambuParts.ModelSettingsPart, out var bytes))
            return new BambuModelSettingsData([], new Dictionary<int, BambuModelObject>(), []);

        try {
            using var stream = new MemoryStream(bytes.ToArray(), writable: false);
            using var reader = XmlReader.Create(stream, Settings);

            reader.MoveToContent();

            if (reader.NodeType is not XmlNodeType.Element || reader.LocalName != "config")
                throw new BambuFormatException(
                    $"'{BambuParts.ModelSettingsPart}' must have a <config> root element."
                );

            return ReadModelSettings(reader);
        } catch (XmlException e) {
            throw new BambuFormatException(
                $"'{BambuParts.ModelSettingsPart}' is not well-formed XML.", e
            );
        }
    }

    static BambuModelSettingsData ReadModelSettings(XmlReader reader) {
        var plates = new List<BambuPlateConfig>();
        var objects = new Dictionary<int, BambuModelObject>();
        var assemblyItems = new List<BambuAssemblyItem>();
        var ordinal = 0;

        if (reader.IsEmptyElement) {
            reader.Skip();
            return new BambuModelSettingsData(plates, objects, assemblyItems);
        }

        reader.Read();

        while (reader.NodeType is not (XmlNodeType.EndElement or XmlNodeType.None)) {
            if (reader.NodeType is not XmlNodeType.Element) {
                reader.Read();
                continue;
            }

            switch (reader.LocalName) {
                case "plate":
                    plates.Add(ReadPlate(reader, ++ordinal));
                    break;
                case "object": {
                        var modelObject = ReadModelObject(reader);
                        objects[modelObject.Id] = modelObject;
                        break;
                    }
                case "assemble":
                    assemblyItems.AddRange(ReadAssemblyItems(reader));
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        if (reader.NodeType is XmlNodeType.EndElement)
            reader.Read();

        return new BambuModelSettingsData(plates, objects, assemblyItems);
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
                    // Real packages use both forms: <metadata type="name">text</metadata>
                    // and <metadata key="plater_name" value="..."/>.
                    var key = reader.GetAttribute("key") ?? reader.GetAttribute("type");
                    string value;

                    if (reader.GetAttribute("value") is { } attributeValue) {
                        value = attributeValue;
                        reader.Skip();
                    } else {
                        value = reader.ReadElementContentAsString();
                    }

                    if (string.IsNullOrEmpty(key))
                        continue;

                    raw[key] = value;

                    if (key is "name" or "plate_name" or "plater_name")
                        name = value;
                    else if (key is "objects")
                        objects = [.. objects, .. ParsePlateObjects(value)];
                } else if (reader.LocalName == "object") {
                    objects.Add(ReadPlateObject(reader));
                    reader.Skip();
                } else if (reader.LocalName == "model_instance") {
                    objects.Add(ReadModelInstance(reader));
                } else {
                    reader.Skip();
                }
            }

            if (reader.NodeType is XmlNodeType.EndElement)
                reader.Read();
        } else {
            reader.Skip();
        }

        // Resolve the index last: real packages declare it as a <metadata type="index">
        // child or a <metadata key="plater_id" value="..."/> child rather than an
        // attribute, and the child loop only merges those into raw.
        var index = raw.TryGetValue("index", out var indexText)
            ? ParsePlateIndex(indexText)
            : raw.TryGetValue("plater_id", out var platerId)
                ? ParsePlateIndex(platerId)
                : ordinal;

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

    static BambuPlateObject ReadModelInstance(XmlReader reader) {
        int? objectId = null;
        int? instanceId = null;
        int? identifyId = null;

        if (reader.IsEmptyElement) {
            reader.Skip();
        } else {
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

                var (key, value) = ReadMetadata(reader);

                if (string.IsNullOrEmpty(key))
                    continue;

                switch (key) {
                    case "object_id":
                        objectId = ParsePositiveId(value, "model instance object id");
                        break;
                    case "instance_id":
                        instanceId = ParseNonNegativeId(value, "model instance id");
                        break;
                    case "identify_id":
                        identifyId = ParsePositiveId(value, "model instance identify id");
                        break;
                }
            }

            if (reader.NodeType is XmlNodeType.EndElement)
                reader.Read();
        }

        if (objectId is null)
            throw new BambuFormatException("A plate <model_instance> declaration is missing a valid object_id metadata value.");

        return new BambuPlateObject {
            ObjectId = objectId.Value,
            InstanceId = instanceId,
            IdentifyId = identifyId,
        };
    }

    static BambuModelObject ReadModelObject(XmlReader reader) {
        var id = ParsePositiveId(reader.GetAttribute("id"), "model settings object id");
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        var parts = new List<BambuModelPart>();

        if (reader.IsEmptyElement) {
            reader.Skip();
        } else {
            reader.Read();

            while (reader.NodeType is not (XmlNodeType.EndElement or XmlNodeType.None)) {
                if (reader.NodeType is not XmlNodeType.Element) {
                    reader.Read();
                    continue;
                }

                switch (reader.LocalName) {
                    case "metadata": {
                            var (key, value) = ReadMetadata(reader);

                            if (!string.IsNullOrEmpty(key))
                                metadata[key] = value;

                            break;
                        }
                    case "part":
                        parts.Add(ReadModelPart(reader));
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            if (reader.NodeType is XmlNodeType.EndElement)
                reader.Read();
        }

        return new BambuModelObject {
            Id = id,
            Name = metadata.GetValueOrDefault("name"),
            Metadata = metadata,
            Parts = parts,
        };
    }

    static BambuModelPart ReadModelPart(XmlReader reader) {
        var id = ParsePositiveId(reader.GetAttribute("id"), "model settings part id");
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        var subtype = reader.GetAttribute("subtype");
        var uuid = reader.GetAttribute("uuid");

        if (reader.IsEmptyElement) {
            reader.Skip();
        } else {
            reader.Read();

            while (reader.NodeType is not (XmlNodeType.EndElement or XmlNodeType.None)) {
                if (reader.NodeType is not XmlNodeType.Element) {
                    reader.Read();
                    continue;
                }

                if (reader.LocalName == "metadata") {
                    var (key, value) = ReadMetadata(reader);

                    if (!string.IsNullOrEmpty(key))
                        metadata[key] = value;
                } else {
                    reader.Skip();
                }
            }

            if (reader.NodeType is XmlNodeType.EndElement)
                reader.Read();
        }

        return new BambuModelPart {
            Id = id,
            Subtype = subtype,
            Uuid = uuid,
            Metadata = metadata,
        };
    }

    static IReadOnlyList<BambuAssemblyItem> ReadAssemblyItems(XmlReader reader) {
        var items = new List<BambuAssemblyItem>();

        if (reader.IsEmptyElement) {
            reader.Skip();
            return items;
        }

        reader.Read();

        while (reader.NodeType is not (XmlNodeType.EndElement or XmlNodeType.None)) {
            if (reader.NodeType is not XmlNodeType.Element) {
                reader.Read();
                continue;
            }

            if (reader.LocalName != "assemble_item") {
                reader.Skip();
                continue;
            }

            var attributes = ReadAttributes(reader);
            items.Add(new BambuAssemblyItem {
                ObjectId = ParsePositiveId(reader.GetAttribute("object_id"), "assembly object id"),
                InstanceId = ParseOptionalNonNegativeId(reader.GetAttribute("instance_id"), "assembly instance id"),
                VolumeId = ParseOptionalNonNegativeId(reader.GetAttribute("volume_id"), "assembly volume id"),
                Attributes = attributes,
            });
            reader.Skip();
        }

        if (reader.NodeType is XmlNodeType.EndElement)
            reader.Read();

        return items;
    }

    static (string? Key, string Value) ReadMetadata(XmlReader reader) {
        var key = reader.GetAttribute("key") ?? reader.GetAttribute("type");

        if (reader.GetAttribute("value") is { } value) {
            reader.Skip();
            return (key, value);
        }

        return (key, reader.ReadElementContentAsString());
    }

    static Dictionary<string, string> ReadAttributes(XmlReader reader) {
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);

        while (reader.MoveToNextAttribute())
            attributes[reader.LocalName] = reader.Value;

        reader.MoveToElement();
        return attributes;
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

    static int ParsePositiveId(string? value, string description) {
        if (string.IsNullOrWhiteSpace(value)
            || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            || id < 1) {
            throw new BambuFormatException($"'{value}' is not a valid {description}.");
        }

        return id;
    }

    static int? ParseOptionalNonNegativeId(string? value, string description) {
        if (value is null)
            return null;

        return ParseNonNegativeId(value, description);
    }

    static int ParseNonNegativeId(string value, string description) {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) || id < 0)
            throw new BambuFormatException($"'{value}' is not a valid {description}.");

        return id;
    }

    static IReadOnlyDictionary<int, BambuSlicePlate> ParseSlicePlates(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files
    ) {
        if (!files.TryGetValue(BambuParts.SliceInfoPart, out var bytes))
            return new Dictionary<int, BambuSlicePlate>();

        try {
            using var stream = new MemoryStream(bytes.ToArray(), writable: false);
            using var reader = XmlReader.Create(stream, Settings);

            reader.MoveToContent();

            if (reader.NodeType is not XmlNodeType.Element || reader.LocalName != "config") {
                throw new BambuFormatException(
                    $"'{BambuParts.SliceInfoPart}' must have a <config> root element."
                );
            }

            var plates = new Dictionary<int, BambuSlicePlate>();
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

                if (reader.LocalName == "plate") {
                    var plate = ReadSlicePlate(reader, ++ordinal);
                    plates[plate.Index] = plate;
                } else {
                    reader.Skip();
                }
            }

            return plates;
        } catch (XmlException e) {
            throw new BambuFormatException(
                $"'{BambuParts.SliceInfoPart}' is not well-formed XML.", e
            );
        }
    }

    static BambuSlicePlate ReadSlicePlate(XmlReader reader, int ordinal) {
        var config = ReadAttributes(reader);
        var objects = new List<BambuSlicedObject>();
        var filaments = new List<BambuFilament>();

        if (!reader.IsEmptyElement) {
            reader.Read();

            while (reader.NodeType is not (XmlNodeType.EndElement or XmlNodeType.None)) {
                if (reader.NodeType is not XmlNodeType.Element) {
                    reader.Read();
                    continue;
                }

                switch (reader.LocalName) {
                    case "metadata": {
                            var (key, value) = ReadMetadata(reader);

                            if (!string.IsNullOrEmpty(key))
                                config[key] = value;

                            break;
                        }
                    case "object":
                        objects.Add(ReadSlicedObject(reader));
                        reader.Skip();
                        break;
                    case "filament":
                        filaments.Add(ReadFilament(reader));
                        reader.Skip();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            if (reader.NodeType is XmlNodeType.EndElement)
                reader.Read();
        } else {
            reader.Skip();
        }

        var index = config.TryGetValue("index", out var indexText)
            ? ParsePlateIndex(indexText)
            : ordinal;

        return new BambuSlicePlate {
            Index = index,
            Config = config,
            Objects = objects,
            Filaments = filaments,
        };
    }

    static BambuSlicedObject ReadSlicedObject(XmlReader reader) {
        var skipped = reader.GetAttribute("skipped");

        if (skipped is not null && !bool.TryParse(skipped, out _))
            throw new BambuFormatException($"'{skipped}' is not a valid sliced object skipped value.");

        return new BambuSlicedObject {
            IdentifyId = ParsePositiveId(reader.GetAttribute("identify_id"), "sliced object identify id"),
            Name = reader.GetAttribute("name"),
            IsSkipped = skipped is not null && bool.Parse(skipped),
        };
    }

    static BambuFilament ReadFilament(XmlReader reader) {
        var properties = ReadAttributes(reader);

        return new BambuFilament {
            Id = ParsePositiveId(reader.GetAttribute("id"), "filament id"),
            Properties = properties,
        };
    }

    static IReadOnlyList<BambuPlate> BuildPlates(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files,
        IReadOnlyList<BambuPlateConfig> plateConfigs,
        IReadOnlyDictionary<int, BambuModelObject> modelObjects,
        IReadOnlyDictionary<int, BambuSlicePlate> slicePlates
    ) {
        var indices = new SortedSet<int>();
        var configsByIndex = new Dictionary<int, BambuPlateConfig>();

        foreach (var config in plateConfigs) {
            configsByIndex[config.Index] = config;
            indices.Add(config.Index);
        }

        foreach (var index in slicePlates.Keys)
            indices.Add(index);

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

        // Real sliced packages associate the G-code through an explicit
        // <metadata key="gcode_file" value="..."/> plate child; it wins over the
        // file naming convention. A configured path must resolve to a package part.
        foreach (var config in plateConfigs) {
            if (config.Raw.GetValueOrDefault("gcode_file") is { Length: > 0 } gcodeFile) {
                if (!files.ContainsKey(gcodeFile))
                    throw new BambuFormatException(
                        $"Plate {config.Index} declares gcode_file '{gcodeFile}', which is missing from the package."
                    );

                gcodePaths[config.Index] = gcodeFile;
            }
        }

        var plates = new List<BambuPlate>();

        foreach (var index in indices) {
            var config = configsByIndex.GetValueOrDefault(index);
            var gcodePart = gcodePaths.GetValueOrDefault(index);

            plates.Add(new BambuPlate {
                Index = index,
                Name = config?.Name,
                Objects = config?.Objects.Select(obj => obj with {
                    Name = obj.Name ?? modelObjects.GetValueOrDefault(obj.ObjectId)?.Name,
                }).ToArray() ?? [],
                SliceInfo = slicePlates.GetValueOrDefault(index),
                Thumbnail = BuildThumbnail(files, index, imagePaths, smallImagePaths),
                GCodePart = gcodePart,
                GCode = gcodePart is null ? null : BambuParts.ReadText(files[gcodePart]),
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

    static ThreeMFPackage RestrictToKnownParts(ThreeMFPackage package, ThreeMFCore core) {
        var known = package.Files
            .Where(file => IsKnownPart(file.Key, core))
            .ToDictionary(file => file.Key, file => file.Value, StringComparer.Ordinal);

        return new ThreeMFPackage {
            Files = known,
            ContentTypes = package.ContentTypes,
            Relationships = package.Relationships,
        };
    }

    static bool IsKnownPart(string path, ThreeMFCore core) {
        return path is ThreeMFSerializer.ContentTypesPath or ThreeMFSerializer.RelationshipsPath
            || core.Parts.ContainsKey(path)
            || core.Parts.Keys.Any(partPath => CoreParser.GetRelationshipsPartPath(partPath) == path)
            || BambuParts.IsRecognizedPart(path);
    }
}
