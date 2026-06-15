namespace Shiron.Lib.Collections.Bucket;

public class BucketStore<TK> : IBucketStore<TK> where TK : IEquatable<TK> {
    private readonly Dictionary<Type, IBucket<TK>> _valueTypes = [];
    private readonly Dictionary<TK, Type> _keyRegistry = [];
    private readonly Dictionary<TK, object?> _referenceTypes = [];

    private TypedBucket<TK, T> GetOrCreate<T>() {
        if (_valueTypes.TryGetValue(typeof(T), out var bucket)) {
            return (TypedBucket<TK, T>) bucket;
        }

        var newBucket = new TypedBucket<TK, T>();
        _valueTypes[typeof(T)] = newBucket;
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
            if (!oldType.IsValueType) {
                _referenceTypes.Remove(key);
            } else if (_valueTypes.TryGetValue(oldType, out var oldBucket)) {
                oldBucket.Remove(key);
            }
        }

        _keyRegistry[key] = newType;

        if (!newType.IsValueType) {
            _referenceTypes[key] = value;
            return;
        }

        GetOrCreate<T>().Set(key, value);
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
            return ((TypedBucket<TK, T>) bucket).Get(key);
        }
        return default;
    }

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
            return ((TypedBucket<TK, T>) bucket).TryGet(key, out value!);
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

    public bool TryGetAny(TK key, out object? value) {
        if (_keyRegistry.TryGetValue(key, out var type)) {
            if (!type.IsValueType) {
                if (_referenceTypes.TryGetValue(key, out var refVal)) {
                    value = refVal;
                    return true;
                }
                value = null;
                return false;
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

        if (!_keyRegistry.TryGetValue(key, out var registeredType) || registeredType != type) {
            return false;
        }

        _keyRegistry.Remove(key);

        if (!type.IsValueType) {
            return _referenceTypes.Remove(key);
        }

        if (_valueTypes.TryGetValue(type, out var bucket)) {
            return ((TypedBucket<TK, T>) bucket).Remove(key);
        }
        return false;
    }

    /// <inheritdoc/>
    public bool RemoveAny(TK key) {
        if (_keyRegistry.Remove(key, out var type)) {
            if (!type.IsValueType) {
                return _referenceTypes.Remove(key);
            }
            if (_valueTypes.TryGetValue(type, out var bucket)) {
                return bucket.Remove(key);
            }
        }
        return false;
    }

    /// <inheritdoc/>
    public bool CanCast<T>(TK key) {
        return _keyRegistry.TryGetValue(key, out var type) && type.IsAssignableTo(typeof(T));
    }

    /// <inheritdoc/>
    public T? GetAs<T>(TK key) {
        return CanCast<T>(key) ? (T?) GetAny(key) : default;
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
