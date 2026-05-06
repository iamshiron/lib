using System.Collections.Concurrent;

namespace Shiron.Lib.Collections.Bucket;

/// <summary>
/// Thread-safe heterogeneous bucket store. Keys are globally unique across all value types;
/// setting a key with a different type automatically evicts the previous entry.
/// </summary>
/// <typeparam name="TK">Key type.</typeparam>
public class ConcurrentBucketStore<TK> where TK : IEquatable<TK> {
    private readonly ConcurrentDictionary<Type, IBucket<TK>> _buckets = [];
    private readonly ConcurrentDictionary<TK, Type> _keyRegistry = [];

    private ConcurrentTypedBucket<TK, T> GetOrCreate<T>() {
        var bucket = _buckets.GetOrAdd(typeof(T), _ => new ConcurrentTypedBucket<TK, T>());
        return (ConcurrentTypedBucket<TK, T>) bucket;
    }

    /// <summary>
    /// Sets or overwrites the typed value for <paramref name="key"/>.
    /// If the key was previously bound to a different type, the old entry is evicted.
    /// </summary>
    public void Set<T>(TK key, T value) {
        var newType = typeof(T);
        _keyRegistry.AddOrUpdate(key, newType, (k, oldType) => {
            if (oldType != newType && _buckets.TryGetValue(oldType, out var oldBucket)) {
                oldBucket.Remove(k);
            }
            return newType;
        });

        GetOrCreate<T>().Set(key, value);
    }

    /// <summary>
    /// Returns the typed value for <paramref name="key"/>, or <c>default</c> if not found.
    /// </summary>
    public T? Get<T>(TK key) {
        if (_buckets.TryGetValue(typeof(T), out var bucket)) {
            return ((ConcurrentTypedBucket<TK, T>) bucket).Get(key);
        }
        return default;
    }

    /// <summary>
    /// Attempts to retrieve the typed value for <paramref name="key"/>.
    /// </summary>
    public bool TryGet<T>(TK key, out T? value) {
        if (_buckets.TryGetValue(typeof(T), out var bucket)) {
            return ((ConcurrentTypedBucket<TK, T>) bucket).TryGet(key, out value!);
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Returns the value for <paramref name="key"/> (boxed), or <c>null</c> if not found.
    /// </summary>
    public object? GetAny(TK key) {
        if (_keyRegistry.TryGetValue(key, out var type) && _buckets.TryGetValue(type, out var bucket)) {
            return bucket.GetAny(key);
        }
        return null;
    }

    /// <summary>
    /// Attempts to retrieve the value for <paramref name="key"/> without knowing its compile-time type.
    /// </summary>
    public bool TryGetAny(TK key, out object? value) {
        if (_keyRegistry.TryGetValue(key, out var type) && _buckets.TryGetValue(type, out var bucket)) {
            return bucket.TryGetAny(key, out value);
        }
        value = null;
        return false;
    }

    /// <summary>
    /// Removes the typed entry for <paramref name="key"/>. Returns <c>true</c> if found.
    /// </summary>
    public bool Remove<T>(TK key) {
        var kvp = new KeyValuePair<TK, Type>(key, typeof(T));
        ICollection<KeyValuePair<TK, Type>> registryAsCollection = _keyRegistry;
        if (registryAsCollection.Remove(kvp)) {
            if (_buckets.TryGetValue(typeof(T), out var bucket)) {
                return ((ConcurrentTypedBucket<TK, T>) bucket).Remove(key);
            }
        }
        return false;
    }

    /// <summary>
    /// Removes any entry for <paramref name="key"/> regardless of its type. Returns <c>true</c> if found.
    /// </summary>
    public bool RemoveAny(TK key) {
        if (_keyRegistry.TryRemove(key, out var type) && _buckets.TryGetValue(type, out var bucket)) {
            return bucket.Remove(key);
        }
        return false;
    }

    /// <summary>
    /// Returns <c>true</c> if the key is bound to a value.
    /// </summary>
    public bool Has<T>(TK key) {
        if (!_keyRegistry.TryGetValue(key, out var type)) return false;
        if (!_buckets.TryGetValue(type, out var bucket)) return false;
        return bucket.Has(key);
    }
    /// <summary>
    /// Returns <c>true</c> if the key is stored in any bucket.
    /// </summary>
    public bool HasAny(TK key) {
        return _keyRegistry.ContainsKey(key);
    }
}
