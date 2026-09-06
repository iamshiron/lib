using Shiron.Lib.Tess.ThreeMF.Detection;
using Shiron.Lib.Tess.ThreeMF.Writing;

namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Holds the document extensions registered on <see cref="ThreeMFSerializerOptions.Extensions"/>.
/// Extensions add candidate handlers and writers to document detection and
/// serialization; they can never replace or suppress the built-in standard and Bambu
/// handlers and writers.
/// </summary>
public sealed class ExtensionCollection {
    readonly List<IThreeMFDocumentHandler> _handlers = [];
    readonly List<IThreeMFDocumentWriter> _writers = [];

    internal IReadOnlyList<IThreeMFDocumentHandler> Handlers => _handlers;

    internal IReadOnlyList<IThreeMFDocumentWriter> Writers => _writers;

    /// <summary>
    /// Registers an additional document extension that is consulted during document
    /// detection and serialization.
    /// </summary>
    /// <param name="extension">The extension to register.</param>
    public void Add(IThreeMFExtension extension) {
        ArgumentNullException.ThrowIfNull(extension);

        _handlers.Add(new ExtensionDocumentHandler(extension));
        _writers.Add(new ExtensionDocumentWriter(extension));
    }
}
