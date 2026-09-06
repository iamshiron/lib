using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml;

namespace Shiron.Lib.Tess.ThreeMF.Internal;

/// <summary>
/// The well-known part names and file naming conventions of the Bambu 3MF package
/// layout, plus the small text parsing helpers shared by detection and parsing.
/// </summary>
internal static class BambuParts {
    public const string HeaderItemPart = "Metadata/header_item";
    public const string SliceInfoPart = "Metadata/slice_info.config";
    public const string ProjectSettingsPart = "Metadata/project_settings.config";
    public const string ModelSettingsPart = "Metadata/model_settings.config";

    const string HeaderItemElement = "header_item";

    static readonly XmlReaderSettings HeaderXmlSettings = new() {
        DtdProcessing = DtdProcessing.Ignore,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreWhitespace = true,
        CloseInput = true,
    };

    static readonly Regex PlateImagePattern =
        new(@"^Metadata/plate_(\d+)\.png$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static readonly Regex PlateSmallImagePattern =
        new(@"^Metadata/plate_(\d+)_small\.png$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static readonly Regex PlateGCodePattern =
        new(@"^Metadata/plate_(\d+)\.gcode$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static readonly Regex PlateFilePattern =
        new(@"^Metadata/plate_\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static readonly char[] KeyValueSeparators = ['=', ':'];

    /// <summary>
    /// Attempts to match the part path of a plate image, e.g.
    /// <c>Metadata/plate_1.png</c>.
    /// </summary>
    public static bool TryMatchPlateImage(string path, out int index) => TryMatch(PlateImagePattern, path, out index);

    /// <summary>
    /// Attempts to match the part path of a small plate image, e.g.
    /// <c>Metadata/plate_1_small.png</c>.
    /// </summary>
    public static bool TryMatchPlateSmallImage(string path, out int index) => TryMatch(PlateSmallImagePattern, path, out index);

    /// <summary>
    /// Attempts to match the part path of a sliced plate G-code file, e.g.
    /// <c>Metadata/plate_1.gcode</c>.
    /// </summary>
    public static bool TryMatchPlateGCode(string path, out int index) => TryMatch(PlateGCodePattern, path, out index);

    /// <summary>
    /// Determines whether the part path is a sliced plate G-code file.
    /// </summary>
    public static bool IsPlateGCodePath(string path) => PlateGCodePattern.IsMatch(path);

    /// <summary>
    /// Determines whether the part path is a Bambu metadata part the parser
    /// recognizes: the header and config parts, or any <c>Metadata/plate_&lt;n&gt;...</c>
    /// file.
    /// </summary>
    public static bool IsRecognizedPart(string path) {
        return path is HeaderItemPart or SliceInfoPart or ProjectSettingsPart or ModelSettingsPart
            || PlateFilePattern.IsMatch(path);
    }

    /// <summary>
    /// Decodes raw part bytes as text, honoring a byte order mark when present.
    /// </summary>
    public static string ReadText(ReadOnlyMemory<byte> bytes) {
        using var stream = MemoryMarshal.TryGetArray(bytes, out var segment)
            ? new MemoryStream(segment.Array!, segment.Offset, segment.Count, writable: false, publiclyVisible: true)
            : new MemoryStream(bytes.ToArray(), writable: false);
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);

        return reader.ReadToEnd();
    }

    /// <summary>
    /// Parses header markers from either supported form of the
    /// <c>Metadata/header_item</c> part: key/value text lines, e.g.
    /// <c>X-BBL-Client-Type = bambu-studio</c>, or XML, e.g.
    /// <c>&lt;header_item key="X-BBL-Client-Type" value="slicer"/&gt;</c>, optionally
    /// wrapped in a container element such as <c>&lt;config&gt;</c>. XML content is
    /// recognized by a leading <c>&lt;</c>. Duplicate keys keep the last value;
    /// malformed XML yields no items, as header probing must never throw.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ParseHeaderItems(string text) {
        return text.TrimStart().StartsWith('<')
            ? ParseHeaderXml(text)
            : ParseHeaderText(text);
    }

    static IReadOnlyDictionary<string, string> ParseHeaderText(string text) {
        var items = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in text.Split('\n')) {
            var line = rawLine.Trim();

            if (line.Length is 0 || line[0] is '#' or ';')
                continue;

            var separator = line.IndexOfAny(KeyValueSeparators);

            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();

            if (key.Length > 0)
                items[key] = line[(separator + 1)..].Trim();
        }

        return items;
    }

    static IReadOnlyDictionary<string, string> ParseHeaderXml(string text) {
        try {
            using var reader = XmlReader.Create(new StringReader(text), HeaderXmlSettings);
            var items = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            while (!reader.EOF) {
                if (reader.NodeType is not XmlNodeType.Element || reader.LocalName != HeaderItemElement) {
                    reader.Read();
                    continue;
                }

                var key = reader.GetAttribute("key");

                if (string.IsNullOrEmpty(key)) {
                    reader.Skip();
                    continue;
                }

                if (reader.GetAttribute("value") is { } value) {
                    items[key] = value;
                    reader.Skip();
                } else {
                    items[key] = reader.ReadElementContentAsString();
                }
            }

            return items;
        } catch (XmlException) {
            // Malformed XML header content yields no items; probing must never throw.
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    static bool TryMatch(Regex pattern, string path, out int index) {
        var match = pattern.Match(path);

        if (match.Success) {
            index = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            return true;
        }

        index = 0;
        return false;
    }
}
