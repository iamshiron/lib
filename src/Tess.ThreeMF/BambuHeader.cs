namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents the header items of a Bambu 3MF package, parsed from the
/// <c>Metadata/header_item</c> part in either its key/value text form or its XML
/// form, e.g. <c>&lt;header_item key="X-BBL-Client-Type" value="slicer"/&gt;</c>.
/// </summary>
public sealed class BambuHeader {
    internal const string ClientTypeKey = "X-BBL-Client-Type";
    internal const string ClientVersionKey = "X-BBL-Client-Version";

    /// <summary>
    /// Gets all header items keyed by marker name, with their raw text values.
    /// Empty when the package does not contain the part.
    /// </summary>
    public required IReadOnlyDictionary<string, string> Items { get; init; }

    /// <summary>
    /// Gets the <c>X-BBL-Client-Type</c> item, e.g. <c>bambu-studio</c>, or
    /// <see langword="null"/> when absent.
    /// </summary>
    public string? ClientType => Items.TryGetValue(ClientTypeKey, out var value) ? value : null;

    /// <summary>
    /// Gets the <c>X-BBL-Client-Version</c> item, e.g. <c>01.09.05.51</c>, or
    /// <see langword="null"/> when absent.
    /// </summary>
    public string? ClientVersion => Items.TryGetValue(ClientVersionKey, out var value) ? value : null;
}
