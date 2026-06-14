using System.Collections.Concurrent;

namespace Shiron.Lib.Collections.Bucket;

public class ArrayBucketStore : IBucketStore<int> {
    public ICollection<int> Keys { get; } = [];

    // TODO: Remove / replace with proper data structure
    public IReadOnlyDictionary<Type, IBucket<int>> ValueTypes { get; } = new Dictionary<Type, IBucket<int>>();

    private readonly ConcurrentDictionary<Type, IArrayBucket> _valueTypes = [];
    private readonly ConcurrentDictionary<int, Type> _keyRegistry = [];
    private readonly TypedArrayBucket<object?> _referenceTypes;

    public ArrayBucketStore(Dictionary<Type, int> bucketSizes) {
        foreach (var (type, size) in bucketSizes) {
            if (type == typeof(object)) {
                _referenceTypes = new TypedArrayBucket<object?>(size);
                continue;
            }
            _valueTypes[type] = Activator.CreateInstance(typeof(TypedArrayBucket<>).MakeGenericType(type), size) as IArrayBucket ??
                throw new InvalidOperationException("Failed to create bucket for type " + type);
        }
        _referenceTypes ??= new TypedArrayBucket<object?>(0);
    }

    private TypedArrayBucket<T> GetBucket<T>() {
        var bucket = _valueTypes.GetValueOrDefault(typeof(T));
        return bucket as TypedArrayBucket<T> ?? throw new InvalidOperationException("No bucket for type " + typeof(T));
    }

    private void EvictPrevious(int key, Type newType) {
        if (_keyRegistry.TryGetValue(key, out var oldType) && oldType != newType) {
            if (!oldType.IsValueType) {
                _referenceTypes.Set(key, null);
            } else if (_valueTypes.TryGetValue(oldType, out var oldBucket)) {
                oldBucket.Clear(key);
            }
        }

        _keyRegistry[key] = newType;
    }

    public void Set<T>(int key, T value) {
        var newType = typeof(T);
        EvictPrevious(key, newType);

        if (!newType.IsValueType) {
            _referenceTypes.Set(key, value);
            return;
        }

        GetBucket<T>().Set(key, value);
    }
    public void Set(int key, object? value, Type type) {
        var method = typeof(ArrayBucketStore)
            .GetMethods()
            .First(m => m.Name == nameof(Set)
                && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 2)
            .MakeGenericMethod(type);

        method.Invoke(this, [key, value]);
    }
    public T? Get<T>(int key) {
        var type = typeof(T);
        if (!type.IsValueType) {
            if (_referenceTypes.TryGetValue(key, out var value) && value is T typed) {
                return typed;
            }
            return default;
        }

        return GetBucket<T>().Get(key);
    }
    public T? GetAs<T>(int key) {
        var bucket = GetBucket<T>();
        return bucket.Get(key);
    }
    public bool TryGet<T>(int key, out T? value) {
        var type = typeof(T);

        if (!type.IsValueType) {
            if (_referenceTypes.TryGetValue(key, out var obj) && obj is T typed) {
                value = typed;
                return true;
            }
            value = default;
            return false;
        }

        var bucket = GetBucket<T>();
        value = bucket.Get(key);
        return value is not null;
    }
    public object? GetAny(int key) {
        if (_keyRegistry.TryGetValue(key, out var type)) {
            if (!type.IsValueType) {
                return _referenceTypes.Get(key);
            }
            if (_valueTypes.TryGetValue(type, out var bucket)) {
                return bucket.GetAny(key);
            }
        }

        return null;
    }
    public bool TryGetAny(int key, out object? value) {
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

    /// <summary>
    /// Not supported.
    /// </summary>
    public bool Remove<T>(int key) {
        throw new NotImplementedException();
    }
    /// <summary>
    /// Not supported.
    /// </summary>
    public bool RemoveAny(int key) {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns true if the key is within the bounds of the bucket.
    /// </summary>
    public bool Has<T>(int key) {
        if (!typeof(T).IsValueType) {
            return key >= 0 && key < _referenceTypes.Size;
        }

        var size = GetBucket<T>().Size;
        return key >= 0 && key < size;
    }
    public bool CanCast<T>(int key) {
        return _keyRegistry.TryGetValue(key, out var type) && type.IsAssignableTo(typeof(T));
    }
    /// <summary>
    /// Always returns true, array buckets are always populated.
    /// </summary>
    public bool HasAny(int key) {
        return true;
    }
    public Type? TypeOf(int key) {
        return _keyRegistry.GetValueOrDefault(key);
    }
}
