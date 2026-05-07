using System.Collections.Concurrent;

namespace Shiron.Lib.Collections.Bucket;

/// <summary>
/// Thread-safe heterogeneous bucket store. Keys are globally unique across all value types;
/// setting a key with a different type automatically evicts the previous entry.
/// Value types are stored in typed buckets; reference types are stored in a shared reference dictionary.
/// </summary>
/// <typeparam name="TK">Key type.</typeparam>
public class ConcurrentBucketStore<TK> : IBucketStore<TK> where TK : IEquatable<TK> {
    private readonly ConcurrentDictionary<Type, IBucket<TK>> _valueTypes = [];
    private readonly ConcurrentDictionary<TK, Type> _keyRegistry = [];
    private readonly ConcurrentDictionary<TK, object?> _referenceTypes = [];

    public ICollection<TK> Keys => _keyRegistry.Keys;
    public IReadOnlyDictionary<Type, IBucket<TK>> ValueTypes => _valueTypes;

    private ConcurrentTypedBucket<TK, T> GetOrCreate<T>() {
        var bucket = _valueTypes.GetOrAdd(typeof(T), _ => new ConcurrentTypedBucket<TK, T>());
        return (ConcurrentTypedBucket<TK, T>) bucket;
    }

    private void EvictPrevious(TK key, Type newType) {
        _keyRegistry.AddOrUpdate(key, newType, (k, oldType) => {
            if (oldType != newType) {
                if (!oldType.IsValueType && _referenceTypes.TryRemove(k, out _)) {
                    return newType;
                }
                if (_valueTypes.TryGetValue(oldType, out var oldBucket)) {
                    oldBucket.Remove(k);
                }
            }
            return newType;
        });
    }

    /// <inheritdoc/>
    public void Set<T>(TK key, T value) {
        var newType = typeof(T);

        if (!newType.IsValueType) {
            EvictPrevious(key, newType);
            _referenceTypes[key] = value;
            _keyRegistry[key] = newType;
            return;
        }

        EvictPrevious(key, newType);
        GetOrCreate<T>().Set(key, value);
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
        var type = typeof(T);

        if (!type.IsValueType) {
            if (_referenceTypes.TryGetValue(key, out var value) && value is T typed) {
                return typed;
            }
            return default;
        }

        if (_valueTypes.TryGetValue(type, out var bucket)) {
            return ((ConcurrentTypedBucket<TK, T>) bucket).Get(key);
        }
        return default;
    }

    /// <inheritdoc/>
    public bool TryGet<T>(TK key, out T? value) {
        var type = typeof(T);

        if (!type.IsValueType) {
            if (_referenceTypes.TryGetValue(key, out var obj) && obj is T typed) {
                value = typed;
                return true;
            }
            value = default;
            return false;
        }

        if (_valueTypes.TryGetValue(type, out var bucket)) {
            return ((ConcurrentTypedBucket<TK, T>) bucket).TryGet(key, out value!);
        }
        value = default;
        return false;
    }

    /// <inheritdoc/>
    public object? GetAny(TK key) {
        if (_keyRegistry.TryGetValue(key, out var type)) {
            if (!type.IsValueType) {
                return _referenceTypes.GetValueOrDefault(key);
            }
            if (_valueTypes.TryGetValue(type, out var bucket)) {
                return bucket.GetAny(key);
            }
        }
        return null;
    }

    /// <inheritdoc/>
    public bool TryGetAny(TK key, out object? value) {
        if (_keyRegistry.TryGetValue(key, out var type)) {
            if (!type.IsValueType) {
                return _referenceTypes.TryGetValue(key, out value);
            }
            if (_valueTypes.TryGetValue(type, out var bucket)) {
                return bucket.TryGetAny(key, out value);
            }
        }
        value = null;
        return false;
    }

    /// <inheritdoc/>
    public bool Remove<T>(TK key) {
        var type = typeof(T);

        if (!type.IsValueType) {
            var kvp = new KeyValuePair<TK, Type>(key, type);
            ICollection<KeyValuePair<TK, Type>> registryAsCollection = _keyRegistry;
            if (registryAsCollection.Remove(kvp)) {
                return _referenceTypes.TryRemove(key, out _);
            }
            return false;
        }

        var typedKvp = new KeyValuePair<TK, Type>(key, type);
        ICollection<KeyValuePair<TK, Type>> typedRegistryAsCollection = _keyRegistry;
        if (typedRegistryAsCollection.Remove(typedKvp)) {
            if (_valueTypes.TryGetValue(type, out var bucket)) {
                return ((ConcurrentTypedBucket<TK, T>) bucket).Remove(key);
            }
        }
        return false;
    }

    /// <inheritdoc/>
    public bool RemoveAny(TK key) {
        if (_keyRegistry.TryRemove(key, out var type)) {
            if (!type.IsValueType) {
                return _referenceTypes.TryRemove(key, out _);
            }
            if (_valueTypes.TryGetValue(type, out var bucket)) {
                return bucket.Remove(key);
            }
        }
        return false;
    }

    /// <inheritdoc/>
    public bool Has<T>(TK key) {
        if (!_keyRegistry.TryGetValue(key, out var type)) return false;
        if (typeof(T) != type) return false;
        if (!type.IsValueType) {
            return _referenceTypes.ContainsKey(key);
        }
        if (_valueTypes.TryGetValue(type, out var bucket)) {
            return bucket.Has(key);
        }
        return false;
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
