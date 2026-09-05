namespace Shiron.Lib.Tess.ThreeMF;

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
