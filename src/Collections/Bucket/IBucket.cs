namespace Shiron.Lib.Collections.Bucket;

/// <summary>
/// Type-erased read/remove interface for a key-addressed bucket store.
/// </summary>
/// <typeparam name="TK">Key type.</typeparam>
public interface IBucket<in TK> where TK : IEquatable<TK> {
    /// <summary>
    /// Attempts to retrieve the value for <paramref name="key"/> without knowing its compile-time type.
    /// </summary>
    bool TryGetAny(TK key, out object? value);

    /// <summary>
    /// Returns the value for <paramref name="key"/> (boxed), or <c>null</c> if not found.
    /// </summary>
    object? GetAny(TK key);

    /// <summary>
    /// Removes the entry for <paramref name="key"/>. Returns <c>true</c> if the key existed.
    /// </summary>
    bool Remove(TK key);

    /// <summary>
    /// Removes all entries.
    /// </summary>
    void Clear();
}
