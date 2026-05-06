namespace Shiron.Lib.Collections.Bucket;

/// <summary>
/// A generic interface for storing key-value pairs in buckets.
/// </summary>
/// <typeparam name="TK">The type of keys used in the bucket store.</typeparam>
public interface IBucketStore<TK> where TK : IEquatable<TK> {
    ICollection<TK> Keys { get; }
    IReadOnlyDictionary<Type, IBucket<TK>> Buckets { get; }

    /// <summary>
    /// Sets the value for <paramref name="key"/> to <paramref name="value"/>.
    /// </summary>
    void Set<T>(TK key, T value);

    /// <summary>
    /// Returns the typed value for <paramref name="key"/>, or <c>default</c> if not found.
    /// </summary>
    T? Get<T>(TK key);

    /// <summary>
    /// Attempts to retrieve the typed value for <paramref name="key"/>.
    /// </summary>
    bool TryGet<T>(TK key, out T? value);

    /// <summary>
    /// Returns the value for <paramref name="key"/> (boxed), or <c>null</c> if not found.
    /// </summary>
    object? GetAny(TK key);
    /// <summary>
    /// Attempts to retrieve the value for <paramref name="key"/> without knowing its compile-time type.
    /// </summary>
    bool TryGetAny(TK key, out object? value);

    /// <summary>
    /// Removes the typed entry for <paramref name="key"/>. Returns <c>true</c> if found.
    /// </summary>
    bool Remove<T>(TK key);

    /// <summary>
    /// Removes any entry for <paramref name="key"/> regardless of its type. Returns <c>true</c> if found.
    /// </summary>
    bool RemoveAny(TK key);

    /// <summary>
    /// Returns <c>true</c> if the key is bound to a value.
    /// </summary>
    bool Has<T>(TK key);

    /// <summary>
    /// Returns <c>true</c> if the key is stored in any bucket.
    /// </summary>
    bool HasAny(TK key);

    /// <summary>
    /// Returns the compile-time type of the value for <paramref name="key"/>.
    /// </summary>
    Type? TypeOf(TK key);
}
