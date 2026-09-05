namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents a mesh <c>&lt;triangle&gt;</c> as indices into the vertices of the
/// enclosing mesh.
/// </summary>
/// <param name="V1">The index of the first vertex.</param>
/// <param name="V2">The index of the second vertex.</param>
/// <param name="V3">The index of the third vertex.</param>
public readonly record struct Triangle(int V1, int V2, int V3);
