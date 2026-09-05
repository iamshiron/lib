namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents a <c>&lt;mesh&gt;</c> of a 3MF object: triangle geometry in object space.
/// </summary>
public sealed class Mesh {
    /// <summary>
    /// Gets the mesh vertices.
    /// </summary>
    public required IReadOnlyList<Vertex> Vertices { get; init; }

    /// <summary>
    /// Gets the mesh triangles as vertex indices. Empty for vertex-only meshes.
    /// </summary>
    public IReadOnlyList<Triangle> Triangles { get; init; } = [];
}
