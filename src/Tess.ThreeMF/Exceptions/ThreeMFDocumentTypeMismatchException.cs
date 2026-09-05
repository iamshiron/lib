namespace Shiron.Lib.Tess.ThreeMF.Exceptions;

/// <summary>
/// Thrown when a document was deserialized successfully but is not assignable to the
/// document type requested through the generic <c>Deserialize</c> overload.
/// </summary>
public sealed class ThreeMFDocumentTypeMismatchException : ThreeMFException {
    /// <summary>
    /// Gets the document type that was requested by the caller.
    /// </summary>
    public Type RequestedType { get; }

    /// <summary>
    /// Gets the runtime document type that was actually deserialized.
    /// </summary>
    public Type ActualType { get; }

    public ThreeMFDocumentTypeMismatchException(Type requestedType, Type actualType)
        : base(
            $"The deserialized 3MF document is of type '{Describe(actualType)}', "
            + $"which is not assignable to the requested type '{Describe(requestedType)}'."
        ) {
        RequestedType = requestedType;
        ActualType = actualType;
    }

    static string Describe(Type type) {
        ArgumentNullException.ThrowIfNull(type);

        return type.FullName ?? type.ToString();
    }
}
