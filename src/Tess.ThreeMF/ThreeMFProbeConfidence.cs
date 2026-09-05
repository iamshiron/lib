namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// The confidence with which an extension claims a package as its document kind.
/// The ordering defines deterministic precedence: a higher value outranks lower ones.
/// </summary>
public enum ThreeMFProbeConfidence {
    /// <summary>The extension does not claim the package.</summary>
    None = 0,

    /// <summary>The package could plausibly be the extension's document kind.</summary>
    Possible = 1,

    /// <summary>Strong evidence that the package is the extension's document kind.</summary>
    Probable = 2,

    /// <summary>The package is definitively the extension's document kind.</summary>
    Certain = 3,
}
