using System.Collections.Concurrent;

namespace Shiron.Lib.Collections.Bucket;

/// <summary>
/// Thread-safe, type-homogeneous bucket backed by a <see cref="ConcurrentDictionary{TK,T}"/>.
/// </summary>
/// <typeparam name="TK">Key type.</typeparam>
/// <typeparam name="T">Value type.</typeparam>
public class ConcurrentTypedBucket<TK, T> : IBucket<TK> where TK : IEquatable<TK> {
    private readonly ConcurrentDictionary<TK, T> _data = [];

    /// <summary>
    /// Sets or overwrites the value for <paramref name="key"/>.
    /// </summary>
    public void Set(TK key, T value) {
        _data[key] = value;
    }

    /// <summary>
    /// Returns the value for <paramref name="key"/>, or <c>default</c> if not found.
    /// </summary>
    public T? Get(TK key) {
        return _data.GetValueOrDefault(key);
    }

    /// <summary>
    /// Attempts to retrieve the value for <paramref name="key"/>.
    /// </summary>
    public bool TryGet(TK key, out T value) {
        return _data.TryGetValue(key, out value!);
    }

    /// <inheritdoc/>
    public bool TryGetAny(TK key, out object? value) {
        if (_data.TryGetValue(key, out var typedValue)) {
            value = typedValue;
            return true;
        }

        value = null;
        return false;
    }

    /// <inheritdoc/>
    public object? GetAny(TK key) {
        return _data.GetValueOrDefault(key);
    }

    /// <inheritdoc/>
    public bool Remove(TK key) {
        return _data.TryRemove(key, out _);
    }

    /// <inheritdoc/>
    public void Clear() {
        _data.Clear();
    }
}
