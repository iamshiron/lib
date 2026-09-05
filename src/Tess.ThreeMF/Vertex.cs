namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents a mesh <c>&lt;vertex&gt;</c> position in object space.
/// </summary>
/// <param name="X">The X coordinate.</param>
/// <param name="Y">The Y coordinate.</param>
/// <param name="Z">The Z coordinate.</param>
public readonly record struct Vertex(double X, double Y, double Z);
