using Shiron.Lib.Tess.ThreeMF.Exceptions;

namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// The strictness of the validation applied while reading a 3MF document.
/// </summary>
public enum ThreeMFValidationMode {
    /// <summary>
    /// Enforces all 3MF specification validation rules; violations throw
    /// <see cref="ThreeMFCoreException"/>.
    /// </summary>
    Standard,

    /// <summary>
    /// Skips validation rules that do not prevent reading the document, such as
    /// unknown object references and triangle indices pointing outside the vertex list.
    /// </summary>
    Lenient,
}
