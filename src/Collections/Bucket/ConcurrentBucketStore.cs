using System.Collections.Concurrent;

namespace Shiron.Lib.Collections.Bucket;

/// <summary>
/// Thread-safe heterogeneous bucket store. Keys are globally unique across all value types;
/// setting a key with a different type automatically evicts the previous entry.
/// </summary>
/// <typeparam name="TK">Key type.</typeparam>
public class ConcurrentBucketStore<TK> : IBucketStore<TK> where TK : IEquatable<TK> {
    private readonly ConcurrentDictionary<Type, IBucket<TK>> _buckets = [];
    private readonly ConcurrentDictionary<TK, Type> _keyRegistry = [];
    public ICollection<TK> Keys => _keyRegistry.Keys;
    public IReadOnlyDictionary<Type, IBucket<TK>> Buckets => _buckets;

    private ConcurrentTypedBucket<TK, T> GetOrCreate<T>() {
        var bucket = _buckets.GetOrAdd(typeof(T), _ => new ConcurrentTypedBucket<TK, T>());
        return (ConcurrentTypedBucket<TK, T>) bucket;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public bool CanCast<T>(TK key) {
        return _keyRegistry.TryGetValue(key, out var type) && type.IsAssignableTo(typeof(T));
    }

    /// <inheritdoc/>
    public T? GetAs<T>(TK key) {
        return CanCast<T>(key) ? (T?) GetAny(key) : default;
    }

    public void Set(TK key, object? value, Type type) {
        var method = typeof(ConcurrentBucketStore<TK>)
            .GetMethods()
            .First(m => m.Name == nameof(Set)
                     && m.IsGenericMethodDefinition
                     && m.GetParameters().Length == 2)
            .MakeGenericMethod(type);

        method.Invoke(this, [key, value]);
    }

    /// <inheritdoc/>
    public T? Get<T>(TK key) {
        if (_buckets.TryGetValue(typeof(T), out var bucket)) {
            return ((ConcurrentTypedBucket<TK, T>) bucket).Get(key);
        }
        return default;
    }

    /// <inheritdoc/>
    public bool TryGet<T>(TK key, out T? value) {
        if (_buckets.TryGetValue(typeof(T), out var bucket)) {
            return ((ConcurrentTypedBucket<TK, T>) bucket).TryGet(key, out value!);
        }
        value = default;
        return false;
    }

    /// <inheritdoc/>
    public object? GetAny(TK key) {
        if (_keyRegistry.TryGetValue(key, out var type) && _buckets.TryGetValue(type, out var bucket)) {
            return bucket.GetAny(key);
        }
        return null;
    }

    /// <inheritdoc/>
    public bool TryGetAny(TK key, out object? value) {
        if (_keyRegistry.TryGetValue(key, out var type) && _buckets.TryGetValue(type, out var bucket)) {
            return bucket.TryGetAny(key, out value);
        }
        value = null;
        return false;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public bool RemoveAny(TK key) {
        if (_keyRegistry.TryRemove(key, out var type) && _buckets.TryGetValue(type, out var bucket)) {
            return bucket.Remove(key);
        }
        return false;
    }

    /// <inheritdoc/>
    public bool Has<T>(TK key) {
        if (!_keyRegistry.TryGetValue(key, out var type)) return false;
        if (!_buckets.TryGetValue(type, out var bucket)) return false;
        return bucket.Has(key);
    }

    /// <inheritdoc/>
    public bool HasAny(TK key) {
        return _keyRegistry.ContainsKey(key);
    }

    /// <inheritdoc/>
    public Type? TypeOf(TK key) {
        return _keyRegistry.GetValueOrDefault(key);
    }
}
