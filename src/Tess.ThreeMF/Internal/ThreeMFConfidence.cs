namespace Shiron.Lib.Tess.ThreeMF.Internal;

/// <summary>
/// The confidence with which a handler claims a package as its document kind.
/// The ordering defines deterministic precedence: a higher value outranks lower ones.
/// </summary>
internal enum ThreeMFConfidence {
    None = 0,
    Possible = 1,
    Likely = 2,
    Authoritative = 3,
}
