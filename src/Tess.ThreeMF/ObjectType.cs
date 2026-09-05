namespace Shiron.Lib.Tess.ThreeMF;

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
