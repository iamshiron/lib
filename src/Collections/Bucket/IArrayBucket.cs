namespace Shiron.Lib.Collections.Bucket;

/// <summary>
/// Type-erased read/remove interface for a key-addressed bucket store.
/// </summary>
public interface IArrayBucket {
    /// <summary>
    /// Attempts to retrieve the value for <paramref name="index"/> without knowing its compile-time type.
    /// </summary>
    bool TryGetAny(int index, out object? value);

    /// <summary>
    /// Returns the value for <paramref name="index"/> (boxed), or <c>null</c> if not found.
    /// </summary>
    object? GetAny(int index);

    /// <summary>
    /// Clears the value at <paramref name="index"/>, resetting it to the default for the bucket's type.
    /// </summary>
    void Clear(int index);

    int Size { get; }
}
