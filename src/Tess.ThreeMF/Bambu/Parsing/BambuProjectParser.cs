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
        var plateDetails = ParsePlateDetails(context.Files, modelSettings.Plates);
        var filamentSequences = ParseFilamentSequences(context.Files);
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
            Cuts = ParseCuts(context.Files),
        };
        var plates = BuildPlates(
            context.Files, modelSettings.Plates, modelSettings.Objects, slicePlates, plateDetails, filamentSequences
        );

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

            return new BambuProjectSettings {
                Values = values,
                Version = values.GetValueOrDefault("version"),
                PrintProfileId = values.GetValueOrDefault("print_settings_id"),
                Printer = new BambuPrinterSettings {
                    Model = values.GetValueOrDefault("printer_model"),
                    Variant = values.GetValueOrDefault("printer_variant"),
                    ProfileId = values.GetValueOrDefault("printer_settings_id"),
                    Technology = values.GetValueOrDefault("printer_technology"),
                    BedType = values.GetValueOrDefault("curr_bed_type"),
                },
                PrintableArea = ParsePoints(values.GetValueOrDefault("printable_area")),
                PrintableHeight = ParseOptionalDouble(values.GetValueOrDefault("printable_height")),
                Filaments = BuildFilamentProfiles(values),
                PurgeMatrix = ParsePurgeMatrix(values),
            };
        } catch (JsonException e) {
            throw new BambuFormatException(
                $"'{BambuParts.ProjectSettingsPart}' is not well-formed JSON.", e
            );
        }
    }

    static IReadOnlyList<BambuPoint> ParsePoints(string? value) {
        var points = new List<BambuPoint>();

        foreach (var point in ParseStringArray(value)) {
            var coordinates = point.Split('x');

            if (coordinates.Length is not 2
                || !double.TryParse(coordinates[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                || !double.TryParse(coordinates[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)) {
                return [];
            }

            points.Add(new BambuPoint(x, y));
        }

        return points;
    }

    static IReadOnlyList<BambuFilamentProfile> BuildFilamentProfiles(IReadOnlyDictionary<string, string> values) {
        var types = ParseStringArray(values.GetValueOrDefault("filament_type"));
        var colors = ParseStringArray(values.GetValueOrDefault("filament_colour"));
        var profileIds = ParseStringArray(values.GetValueOrDefault("filament_settings_id"));
        var trayIds = ParseStringArray(values.GetValueOrDefault("filament_ids"));
        var vendors = ParseStringArray(values.GetValueOrDefault("filament_vendor"));
        var temperatures = ParseStringArray(values.GetValueOrDefault("nozzle_temperature"));
        var count = new[] {
            types.Count,
            colors.Count,
            profileIds.Count,
            trayIds.Count,
            vendors.Count,
            temperatures.Count,
        }.Max();

        if (count is 0)
            return [];

        var profiles = new BambuFilamentProfile[count];

        for (var index = 0; index < profiles.Length; index++) {
            profiles[index] = new BambuFilamentProfile {
                Index = index,
                MaterialType = ValueAt(types, index),
                Color = ValueAt(colors, index),
                ProfileId = ValueAt(profileIds, index),
                TrayId = ValueAt(trayIds, index),
                Vendor = ValueAt(vendors, index),
                NozzleTemperature = ParseOptionalDouble(ValueAt(temperatures, index)),
            };
        }

        return profiles;
    }

    static string? ValueAt(IReadOnlyList<string> values, int index) {
        return (uint) index < (uint) values.Count ? values[index] : null;
    }

    static BambuPurgeMatrix? ParsePurgeMatrix(IReadOnlyDictionary<string, string> values) {
        var volumes = ParseStringArray(values.GetValueOrDefault("flush_volumes_matrix"));

        if (volumes.Count is 0)
            return null;

        var size = (int) Math.Sqrt(volumes.Count);

        if (size * size != volumes.Count)
            return null;

        var parsed = new double[volumes.Count];

        for (var index = 0; index < parsed.Length; index++) {
            if (!double.TryParse(volumes[index], NumberStyles.Float, CultureInfo.InvariantCulture, out parsed[index]))
                return null;
        }

        return new BambuPurgeMatrix { Size = size, Volumes = parsed };
    }

    static IReadOnlyList<string> ParseStringArray(string? value) {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        try {
            using var document = JsonDocument.Parse(value);

            if (document.RootElement.ValueKind is not JsonValueKind.Array)
                return [];

            return document.RootElement
                .EnumerateArray()
                .Select(item => item.ValueKind is JsonValueKind.String ? item.GetString() : item.GetRawText())
                .OfType<string>()
                .ToArray();
        } catch (JsonException) {
            return [];
        }
    }

    static double? ParseOptionalDouble(string? value) {
        return value is not null && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
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
        var nozzles = new List<BambuNozzle>();
        var amsTimings = new List<BambuAmsTiming>();
        var layerFilaments = new List<BambuLayerFilaments>();

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
                    case "nozzle":
                        nozzles.Add(ReadNozzle(reader));
                        reader.Skip();
                        break;
                    case "ams_list":
                        amsTimings.AddRange(ReadAmsTimings(reader));
                        break;
                    case "layer_filament_lists":
                        layerFilaments.AddRange(ReadLayerFilaments(reader));
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
            PrinterModelId = config.GetValueOrDefault("printer_model_id"),
            EstimatedPrintTime = ParseOptionalDouble(config.GetValueOrDefault("prediction")) is { } prediction
                ? TimeSpan.FromSeconds(prediction)
                : null,
            WeightGrams = ParseOptionalDouble(config.GetValueOrDefault("weight")),
            FirstLayerTimeSeconds = ParseOptionalDouble(config.GetValueOrDefault("first_layer_time")),
            NozzleDiameters = ParseDoubleList(config.GetValueOrDefault("nozzle_diameters")),
            IsOutside = ParseOptionalBoolean(config.GetValueOrDefault("outside")),
            IsSupportUsed = ParseOptionalBoolean(config.GetValueOrDefault("support_used")),
            FilamentMap = ParseIntegerList(config.GetValueOrDefault("filament_maps")),
            Nozzles = nozzles,
            AmsTimings = amsTimings,
            LayerFilaments = layerFilaments,
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

    static BambuNozzle ReadNozzle(XmlReader reader) {
        return new BambuNozzle(
            ParseNonNegativeId(reader.GetAttribute("id") ?? string.Empty, "nozzle id"),
            ParsePositiveId(reader.GetAttribute("extruder_id"), "nozzle extruder id"),
            ParseRequiredDouble(reader.GetAttribute("nozzle_diameter"), "nozzle diameter"),
            reader.GetAttribute("volume_type")
        );
    }

    static IReadOnlyList<BambuAmsTiming> ReadAmsTimings(XmlReader reader) {
        var timings = new List<BambuAmsTiming>();

        if (reader.IsEmptyElement) {
            reader.Skip();
            return timings;
        }

        reader.Read();

        while (reader.NodeType is not (XmlNodeType.EndElement or XmlNodeType.None)) {
            if (reader.NodeType is not XmlNodeType.Element) {
                reader.Read();
                continue;
            }

            if (reader.LocalName == "ams") {
                timings.Add(new BambuAmsTiming(
                    reader.GetAttribute("ams_type"),
                    ParseOptionalDouble(reader.GetAttribute("load_time")),
                    ParseOptionalDouble(reader.GetAttribute("unload_time"))
                ));
            }

            reader.Skip();
        }

        if (reader.NodeType is XmlNodeType.EndElement)
            reader.Read();

        return timings;
    }

    static IReadOnlyList<BambuLayerFilaments> ReadLayerFilaments(XmlReader reader) {
        var entries = new List<BambuLayerFilaments>();

        if (reader.IsEmptyElement) {
            reader.Skip();
            return entries;
        }

        reader.Read();

        while (reader.NodeType is not (XmlNodeType.EndElement or XmlNodeType.None)) {
            if (reader.NodeType is not XmlNodeType.Element) {
                reader.Read();
                continue;
            }

            if (reader.LocalName == "layer_filament_list") {
                entries.Add(new BambuLayerFilaments(
                    ParseIntegerList(reader.GetAttribute("filament_list")),
                    reader.GetAttribute("layer_ranges") ?? string.Empty
                ));
            }

            reader.Skip();
        }

        if (reader.NodeType is XmlNodeType.EndElement)
            reader.Read();

        return entries;
    }

    static IReadOnlyList<double> ParseDoubleList(string? value) {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        var values = new List<double>();

        foreach (var item in value.Split(',', ' ', '\t').Where(item => item.Length > 0)) {
            if (!double.TryParse(item, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                return [];

            values.Add(parsed);
        }

        return values;
    }

    static IReadOnlyList<int> ParseIntegerList(string? value) {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        var values = new List<int>();

        foreach (var item in value.Split(',', ' ', '\t').Where(item => item.Length > 0)) {
            if (!int.TryParse(item, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                return [];

            values.Add(parsed);
        }

        return values;
    }

    static bool? ParseOptionalBoolean(string? value) {
        return value is not null && bool.TryParse(value, out var parsed) ? parsed : null;
    }

    static double ParseRequiredDouble(string? value, string description) {
        if (value is null || !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            throw new BambuFormatException($"'{value}' is not a valid {description}.");

        return parsed;
    }

    static IReadOnlyDictionary<int, BambuPlateDetails> ParsePlateDetails(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files,
        IReadOnlyList<BambuPlateConfig> configs
    ) {
        var paths = new Dictionary<int, string>();

        foreach (var path in files.Keys) {
            if (BambuParts.TryMatchPlateDetails(path, out var index))
                paths[index] = path;
        }

        foreach (var config in configs) {
            if (config.Raw.GetValueOrDefault("pattern_bbox_file") is not { Length: > 0 } path)
                continue;

            if (!files.ContainsKey(path)) {
                throw new BambuFormatException(
                    $"Plate {config.Index} declares pattern_bbox_file '{path}', which is missing from the package."
                );
            }

            paths[config.Index] = path;
        }

        var details = new Dictionary<int, BambuPlateDetails>();

        foreach (var (index, path) in paths)
            details[index] = ReadPlateDetails(files[path], index, path);

        return details;
    }

    static BambuPlateDetails ReadPlateDetails(ReadOnlyMemory<byte> bytes, int index, string path) {
        try {
            using var document = JsonDocument.Parse(BambuParts.ReadText(bytes));
            var root = document.RootElement;

            if (root.ValueKind is not JsonValueKind.Object)
                throw new BambuFormatException($"'{path}' must contain a JSON object at the root.");

            return new BambuPlateDetails {
                Index = index,
                Bounds = ReadBounds(root, "bbox_all"),
                Objects = ReadPlateObjectBounds(root),
                BedType = ReadString(root, "bed_type"),
                FilamentColors = ReadJsonStrings(root, "filament_colors"),
                FilamentIds = ReadJsonIntegers(root, "filament_ids"),
                FirstExtruder = ReadInteger(root, "first_extruder"),
                FirstLayerTimeSeconds = ReadDouble(root, "first_layer_time"),
                IsSequentialPrint = ReadBoolean(root, "is_seq_print"),
                NozzleDiameter = ReadDouble(root, "nozzle_diameter"),
            };
        } catch (JsonException e) {
            throw new BambuFormatException($"'{path}' is not well-formed JSON.", e);
        }
    }

    static IReadOnlyList<BambuPlateObjectBounds> ReadPlateObjectBounds(JsonElement root) {
        if (!root.TryGetProperty("bbox_objects", out var elements) || elements.ValueKind is not JsonValueKind.Array)
            return [];

        var objects = new List<BambuPlateObjectBounds>();

        foreach (var element in elements.EnumerateArray()) {
            if (element.ValueKind is not JsonValueKind.Object || ReadInteger(element, "id") is not { } id)
                throw new BambuFormatException("A plate bbox_objects entry is missing a valid id.");

            objects.Add(new BambuPlateObjectBounds {
                Id = id,
                Name = ReadString(element, "name"),
                Bounds = ReadBounds(element, "bbox"),
                Area = ReadDouble(element, "area"),
                LayerHeight = ReadDouble(element, "layer_height"),
            });
        }

        return objects;
    }

    static IReadOnlyDictionary<int, BambuFilamentSequence> ParseFilamentSequences(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files
    ) {
        if (!files.TryGetValue(BambuParts.FilamentSequencePart, out var bytes))
            return new Dictionary<int, BambuFilamentSequence>();

        try {
            using var document = JsonDocument.Parse(BambuParts.ReadText(bytes));

            if (document.RootElement.ValueKind is JsonValueKind.Array && document.RootElement.GetArrayLength() is 0)
                return new Dictionary<int, BambuFilamentSequence>();

            if (document.RootElement.ValueKind is not JsonValueKind.Object) {
                throw new BambuFormatException(
                    $"'{BambuParts.FilamentSequencePart}' must contain a JSON object at the root."
                );
            }

            var sequences = new Dictionary<int, BambuFilamentSequence>();

            foreach (var property in document.RootElement.EnumerateObject()) {
                const string prefix = "plate_";

                if (!property.Name.StartsWith(prefix, StringComparison.Ordinal)
                    || !int.TryParse(property.Name[prefix.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
                    || index < 0
                    || property.Value.ValueKind is not JsonValueKind.Object) {
                    continue;
                }

                sequences[index] = new BambuFilamentSequence {
                    Index = index,
                    NozzleSequence = ReadJsonIntegers(property.Value, "nozzle_sequence"),
                    OptimalAssignment = ReadJsonIntegers(property.Value, "optimal_assignment"),
                    Sequence = ReadJsonIntegers(property.Value, "sequence"),
                };
            }

            return sequences;
        } catch (JsonException e) {
            throw new BambuFormatException($"'{BambuParts.FilamentSequencePart}' is not well-formed JSON.", e);
        }
    }

    static IReadOnlyList<BambuCut> ParseCuts(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) {
        if (!files.TryGetValue(BambuParts.CutInformationPart, out var bytes))
            return [];

        try {
            using var stream = new MemoryStream(bytes.ToArray(), writable: false);
            using var reader = XmlReader.Create(stream, Settings);
            reader.MoveToContent();

            if (reader.NodeType is not XmlNodeType.Element || reader.LocalName != "objects")
                throw new BambuFormatException($"'{BambuParts.CutInformationPart}' must have an <objects> root element.");

            var cuts = new List<BambuCut>();

            if (reader.IsEmptyElement)
                return cuts;

            reader.Read();

            while (reader.NodeType is not (XmlNodeType.EndElement or XmlNodeType.None)) {
                if (reader.NodeType is not XmlNodeType.Element) {
                    reader.Read();
                    continue;
                }

                if (reader.LocalName != "object") {
                    reader.Skip();
                    continue;
                }

                var objectId = ParsePositiveId(reader.GetAttribute("id"), "cut object id");

                if (reader.IsEmptyElement) {
                    reader.Skip();
                    continue;
                }

                reader.Read();

                while (reader.NodeType is not (XmlNodeType.EndElement or XmlNodeType.None)) {
                    if (reader.NodeType is not XmlNodeType.Element) {
                        reader.Read();
                        continue;
                    }

                    if (reader.LocalName == "cut_id") {
                        cuts.Add(new BambuCut(
                            objectId,
                            ParseNonNegativeId(reader.GetAttribute("id") ?? string.Empty, "cut id"),
                            ParseNonNegativeId(reader.GetAttribute("check_sum") ?? string.Empty, "cut checksum"),
                            ParseNonNegativeId(reader.GetAttribute("connectors_cnt") ?? string.Empty, "cut connector count")
                        ));
                    }

                    reader.Skip();
                }

                if (reader.NodeType is XmlNodeType.EndElement)
                    reader.Read();
            }

            return cuts;
        } catch (XmlException e) {
            throw new BambuFormatException($"'{BambuParts.CutInformationPart}' is not well-formed XML.", e);
        }
    }

    static BambuBounds? ReadBounds(JsonElement element, string name) {
        if (!element.TryGetProperty(name, out var values) || values.ValueKind is not JsonValueKind.Array)
            return null;

        var coordinates = values.EnumerateArray().ToArray();

        if (coordinates.Length is not 4 || coordinates.Any(value => !value.TryGetDouble(out _)))
            return null;

        return new BambuBounds(
            coordinates[0].GetDouble(), coordinates[1].GetDouble(), coordinates[2].GetDouble(), coordinates[3].GetDouble()
        );
    }

    static IReadOnlyList<string> ReadJsonStrings(JsonElement element, string name) {
        if (!element.TryGetProperty(name, out var values) || values.ValueKind is not JsonValueKind.Array)
            return [];

        return values.EnumerateArray()
            .Where(value => value.ValueKind is JsonValueKind.String)
            .Select(value => value.GetString() ?? string.Empty)
            .ToArray();
    }

    static IReadOnlyList<int> ReadJsonIntegers(JsonElement element, string name) {
        if (!element.TryGetProperty(name, out var values) || values.ValueKind is not JsonValueKind.Array)
            return [];

        var integers = new List<int>();

        foreach (var value in values.EnumerateArray()) {
            if (!value.TryGetInt32(out var integer))
                return [];

            integers.Add(integer);
        }

        return integers;
    }

    static string? ReadString(JsonElement element, string name) {
        return element.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.String
            ? value.GetString()
            : null;
    }

    static int? ReadInteger(JsonElement element, string name) {
        return element.TryGetProperty(name, out var value) && value.TryGetInt32(out var integer) ? integer : null;
    }

    static double? ReadDouble(JsonElement element, string name) {
        return element.TryGetProperty(name, out var value) && value.TryGetDouble(out var number) ? number : null;
    }

    static bool? ReadBoolean(JsonElement element, string name) {
        return element.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;
    }

    static IReadOnlyList<BambuPlate> BuildPlates(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files,
        IReadOnlyList<BambuPlateConfig> plateConfigs,
        IReadOnlyDictionary<int, BambuModelObject> modelObjects,
        IReadOnlyDictionary<int, BambuSlicePlate> slicePlates,
        IReadOnlyDictionary<int, BambuPlateDetails> plateDetails,
        IReadOnlyDictionary<int, BambuFilamentSequence> filamentSequences
    ) {
        var indices = new SortedSet<int>();
        var configsByIndex = new Dictionary<int, BambuPlateConfig>();

        foreach (var config in plateConfigs) {
            configsByIndex[config.Index] = config;
            indices.Add(config.Index);
        }

        foreach (var index in slicePlates.Keys)
            indices.Add(index);

        foreach (var index in plateDetails.Keys)
            indices.Add(index);

        foreach (var index in filamentSequences.Keys)
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
                Details = plateDetails.GetValueOrDefault(index),
                FilamentSequence = filamentSequences.GetValueOrDefault(index),
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
