using Shiron.Lib.Tess.ThreeMF.Internal;

namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Holds the document handler extensions registered on <see cref="ThreeMFSerializerOptions"/>.
/// Extensions add candidate handlers to document detection; they can never replace or
/// suppress the built-in standard handler.
/// </summary>
public sealed class ThreeMFExtensions {
    readonly List<IThreeMFDocumentHandler> _handlers = [];

    internal IReadOnlyList<IThreeMFDocumentHandler> Handlers => _handlers;

    /// <summary>
    /// Registers an additional document handler that is consulted during document detection.
    /// </summary>
    /// <param name="handler">The handler to register.</param>
    internal void Add(IThreeMFDocumentHandler handler) {
        ArgumentNullException.ThrowIfNull(handler);

        _handlers.Add(handler);
    }
}
