using System.Diagnostics.CodeAnalysis;

namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents the <c>&lt;build&gt;</c> section of a 3MF model: the items to fabricate.
/// </summary>
public sealed class Build {
    /// <summary>
    /// Gets the build items. Empty when the model builds nothing.
    /// </summary>
    public IReadOnlyList<BuildItem> Items { get; init; } = [];
}

/// <summary>
/// Represents a build <c>&lt;item&gt;</c> of a 3MF model: a reference to an object
/// resource with an optional transform applied to it.
/// </summary>
public sealed record BuildItem {
    /// <summary>
    /// Gets the resource id of the object to build.
    /// </summary>
    public required int ObjectId { get; init; }

    /// <summary>
    /// Gets the transform applied to the built object,
    /// or <see langword="null"/> when the item omits the attribute.
    /// </summary>
    public Transform? Transform { get; init; }

    /// <summary>
    /// Gets the part number of the item,
    /// or <see langword="null"/> when the item omits the attribute.
    /// </summary>
    public string? PartNumber { get; init; }
}

/// <summary>
/// Represents a <c>&lt;component&gt;</c> of a 3MF object: a reference to another object
/// resource with an optional transform applied to it.
/// </summary>
public sealed record Component {
    /// <summary>
    /// Gets the resource id of the referenced object.
    /// </summary>
    public required int ObjectId { get; init; }

    /// <summary>
    /// Gets the transform applied to the referenced object,
    /// or <see langword="null"/> when the component omits the attribute.
    /// </summary>
    public Transform? Transform { get; init; }
}

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

/// <summary>
/// Represents a <c>&lt;metadata&gt;</c> entry of a 3MF model, e.g.
/// <c>&lt;metadata name="Title"&gt;My Part&lt;/metadata&gt;</c>.
/// </summary>
public sealed record Metadata {
    /// <summary>
    /// Gets the metadata name, e.g. <c>Title</c>.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the metadata value text.
    /// </summary>
    public required string Value { get; init; }
}

/// <summary>
/// Represents the contents of the <c>&lt;model&gt;</c> root element of a 3MF core
/// model part.
/// </summary>
public sealed class Model {
    /// <summary>
    /// Gets the unit of measure of the model geometry.
    /// Defaults to <see cref="Unit.Millimeter"/>.
    /// </summary>
    public Unit Unit { get; init; } = Unit.Millimeter;

    /// <summary>
    /// Gets the <c>xml:lang</c> language tag of the model,
    /// or <see langword="null"/> when the model omits the attribute.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Gets the metadata entries of the model. Empty when the model declares none.
    /// </summary>
    public IReadOnlyList<Metadata> Metadata { get; init; } = [];

    /// <summary>
    /// Gets the object resources of the model.
    /// </summary>
    public required Resources Resources { get; init; }

    /// <summary>
    /// Gets the build section of the model. Empty when the model builds nothing.
    /// </summary>
    public Build Build { get; init; } = new();
}

/// <summary>
/// Represents an <c>&lt;object&gt;</c> resource of a 3MF model: either a mesh or a
/// composition of components referencing other objects.
/// </summary>
public sealed class Object {
    /// <summary>
    /// Gets the unique positive resource id of the object within the model.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the purpose of the object. Defaults to <see cref="ObjectType.Model"/>.
    /// </summary>
    public ObjectType Type { get; init; } = ObjectType.Model;

    /// <summary>
    /// Gets the object name,
    /// or <see langword="null"/> when the object omits the attribute.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the object part number,
    /// or <see langword="null"/> when the object omits the attribute.
    /// </summary>
    public string? PartNumber { get; init; }

    /// <summary>
    /// Gets the mesh geometry of the object,
    /// or <see langword="null"/> when the object is composed of components instead.
    /// </summary>
    public Mesh? Mesh { get; init; }

    /// <summary>
    /// Gets the component references of the object. Empty for mesh objects.
    /// </summary>
    public IReadOnlyList<Component> Components { get; init; } = [];
}

/// <summary>
/// The purpose of a 3MF object resource, from the <c>type</c> attribute of the
/// <c>&lt;object&gt;</c> element.
/// </summary>
public enum ObjectType {
    /// <summary>The object is part of the output geometry. The 3MF default.</summary>
    Model,

    /// <summary>The object is support material and excluded from the output geometry.</summary>
    Support,

    /// <summary>
    /// The object holds no printable geometry; it must not be referenced by build items
    /// or components.
    /// </summary>
    Other,
}

/// <summary>
/// Represents the <c>&lt;resources&gt;</c> section of a 3MF model.
/// </summary>
public sealed class Resources {
    /// <summary>
    /// Gets the object resources in document order.
    /// </summary>
    public required IReadOnlyList<Object> Objects { get; init; }

    /// <summary>
    /// Attempts to get the object resource with the given id.
    /// </summary>
    /// <param name="id">The object resource id to look for.</param>
    /// <param name="resource">The matching object, when found.</param>
    /// <returns><see langword="true"/> if an object with the id exists.</returns>
    public bool TryGetObject(int id, [NotNullWhen(true)] out Object? resource) {
        foreach (var obj in Objects) {
            if (obj.Id == id) {
                resource = obj;
                return true;
            }
        }

        resource = null;
        return false;
    }
}

/// <summary>
/// Represents a 3MF row-major 4x3 affine transform: the first three rows form the
/// linear part and the fourth row (<c>M3x</c>) is the translation.
/// </summary>
/// <remarks>
/// Serialized as twelve whitespace-separated numbers in the 3MF
/// <c>transform</c> attribute, e.g. <c>1 0 0 0 1 0 0 0 1 10 20 30</c>.
/// </remarks>
public readonly record struct Transform(
    double M00, double M01, double M02,
    double M10, double M11, double M12,
    double M20, double M21, double M22,
    double M30, double M31, double M32
) {
    /// <summary>
    /// Gets the identity transform (no rotation, no translation).
    /// </summary>
    public static Transform Identity { get; } =
        new(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0);
}

/// <summary>
/// Represents a mesh <c>&lt;triangle&gt;</c> as indices into the vertices of the
/// enclosing mesh.
/// </summary>
/// <param name="V1">The index of the first vertex.</param>
/// <param name="V2">The index of the second vertex.</param>
/// <param name="V3">The index of the third vertex.</param>
public readonly record struct Triangle(int V1, int V2, int V3);

/// <summary>
/// The unit of measure of a 3MF model, from the <c>unit</c> attribute of the
/// <c>&lt;model&gt;</c> element.
/// </summary>
public enum Unit {
    /// <summary>One millionth of a meter.</summary>
    Micron,

    /// <summary>One thousandth of a meter. The 3MF default.</summary>
    Millimeter,

    /// <summary>One hundredth of a meter.</summary>
    Centimeter,

    /// <summary>One inch (2.54 centimeters).</summary>
    Inch,

    /// <summary>One foot (12 inches).</summary>
    Foot,

    /// <summary>One meter.</summary>
    Meter,
}

/// <summary>
/// Represents a mesh <c>&lt;vertex&gt;</c> position in object space.
/// </summary>
/// <param name="X">The X coordinate.</param>
/// <param name="Y">The Y coordinate.</param>
/// <param name="Z">The Z coordinate.</param>
public readonly record struct Vertex(double X, double Y, double Z);
