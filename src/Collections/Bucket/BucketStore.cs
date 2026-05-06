namespace Shiron.Lib.Collections.Bucket;

public class BucketStore<TK> : IBucketStore<TK> where TK : IEquatable<TK> {
    private readonly Dictionary<Type, IBucket<TK>> _buckets = [];
    private readonly Dictionary<TK, Type> _keyRegistry = [];
    public ICollection<TK> Keys => _keyRegistry.Keys;
    public IReadOnlyDictionary<Type, IBucket<TK>> Buckets => _buckets;

    private TypedBucket<TK, T> GetOrCreate<T>() {
        if (_buckets.TryGetValue(typeof(T), out var bucket)) {
            return (TypedBucket<TK, T>) bucket;
        }

        var newBucket = new TypedBucket<TK, T>();
        _buckets[typeof(T)] = newBucket;
        return newBucket;
    }

    public void Set(TK key, object? value, Type type) {
        var method = typeof(BucketStore<TK>)
            .GetMethods()
            .First(m => m.Name == nameof(Set)
                     && m.IsGenericMethodDefinition
                     && m.GetParameters().Length == 2)
            .MakeGenericMethod(type);

        method.Invoke(this, [key, value]);
    }

    /// <inheritdoc/>
    public void Set<T>(TK key, T value) {
        var newType = typeof(T);
        if (_keyRegistry.TryGetValue(key, out var oldType) && oldType != newType) {
            if (_buckets.TryGetValue(oldType, out var oldBucket)) {
                oldBucket.Remove(key);
            }
        }

        _keyRegistry[key] = newType;
        GetOrCreate<T>().Set(key, value);
    }

    /// <inheritdoc/>
    public T? Get<T>(TK key) {
        if (_buckets.TryGetValue(typeof(T), out var bucket)) {
            return ((TypedBucket<TK, T>) bucket).Get(key);
        }
        return default;
    }

    public bool TryGet<T>(TK key, out T? value) {
        if (_buckets.TryGetValue(typeof(T), out var bucket)) {
            return ((TypedBucket<TK, T>) bucket).TryGet(key, out value!);
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

    public bool TryGetAny(TK key, out object? value) {
        if (_keyRegistry.TryGetValue(key, out var type) && _buckets.TryGetValue(type, out var bucket)) {
            return bucket.TryGetAny(key, out value);
        }
        value = null;
        return false;
    }

    /// <inheritdoc/>
    public bool Remove<T>(TK key) {
        if (_keyRegistry.TryGetValue(key, out var type) && type == typeof(T)) {
            _keyRegistry.Remove(key);
            if (_buckets.TryGetValue(typeof(T), out var bucket)) {
                return ((TypedBucket<TK, T>) bucket).Remove(key);
            }
        }
        return false;
    }

    /// <inheritdoc/>
    public bool RemoveAny(TK key) {
        if (_keyRegistry.Remove(key, out var type) && _buckets.TryGetValue(type, out var bucket)) {
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
