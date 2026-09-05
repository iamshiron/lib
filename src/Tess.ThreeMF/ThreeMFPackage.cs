namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents the raw contents of a 3MF (OPC) package: every file part plus the
/// package-level OPC metadata parsed from <c>[Content_Types].xml</c> and <c>_rels/.rels</c>.
/// </summary>
public sealed class ThreeMFPackage {
    /// <summary>
    /// Gets all file parts in the package, keyed by part path.
    /// </summary>
    public required IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Files { get; init; }

    /// <summary>
    /// Gets the package-level relationships declared in <c>_rels/.rels</c>.
    /// Empty when the package does not contain that part.
    /// </summary>
    public IReadOnlyList<ThreeMFRelationship> Relationships { get; init; } = [];

    /// <summary>
    /// Gets the content-type declarations from <c>[Content_Types].xml</c>.
    /// Empty when the package does not contain that part.
    /// </summary>
    public IReadOnlyList<ThreeMFContentType> ContentTypes { get; init; } = [];

    /// <summary>
    /// Determines whether the package contains a file with the given path.
    /// </summary>
    /// <param name="file">The part path to look for.</param>
    /// <returns><see langword="true"/> if the package contains the file.</returns>
    public bool Contains(string file) {
        return Files.ContainsKey(file);
    }

    /// <summary>
    /// Gets the raw bytes of the file with the given path.
    /// </summary>
    /// <param name="file">The part path to get.</param>
    /// <returns>The raw file contents.</returns>
    public ReadOnlyMemory<byte> Get(string file) {
        return Files[file];
    }

    /// <summary>
    /// Attempts to get the raw bytes of the file with the given path.
    /// </summary>
    /// <param name="file">The part path to get.</param>
    /// <param name="bytes">The raw file contents, when found.</param>
    /// <returns><see langword="true"/> if the package contains the file.</returns>
    public bool TryGet(string file, out ReadOnlyMemory<byte> bytes) {
        return Files.TryGetValue(file, out bytes);
    }
}
