namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents a single content-type declaration from the <c>[Content_Types].xml</c> part
/// of a 3MF package.
/// </summary>
/// <remarks>
/// A declaration is either a <c>&lt;Default&gt;</c> rule keyed by file extension or an
/// <c>&lt;Override&gt;</c> rule keyed by absolute part name.
/// </remarks>
public sealed record ThreeMFContentType {
    /// <summary>
    /// Gets the MIME content type applied by this declaration.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Gets the file extension (without a leading dot) matched by this <c>&lt;Default&gt;</c>
    /// rule, or <see langword="null"/> for <c>&lt;Override&gt;</c> rules.
    /// </summary>
    public string? Extension { get; init; }

    /// <summary>
    /// Gets the absolute part path matched by this <c>&lt;Override&gt;</c> rule,
    /// or <see langword="null"/> for <c>&lt;Default&gt;</c> rules.
    /// </summary>
    public string? PartName { get; init; }

    /// <summary>
    /// Gets a value indicating whether this declaration overrides the content type
    /// of a specific part.
    /// </summary>
    public bool IsOverride => PartName is not null;
}
