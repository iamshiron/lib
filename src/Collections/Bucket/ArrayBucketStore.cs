using System.Collections.Concurrent;

namespace Shiron.Lib.Collections.Bucket;

/// <summary>
/// A typed, pre-allocated key-value store backed by one array per registered value type plus a
/// shared array for all reference types. Keys are integers in <c>[0, size)</c> per type.
/// </summary>
/// <remarks>
/// Not interchangeable with <see cref="BucketStore{TK}"/>: the type set and key space are fixed at
/// construction, entries cannot be removed, and value types that were not registered have no storage.
/// </remarks>
public sealed class ArrayBucketStore {
    private readonly Dictionary<Type, IArrayBucket> _valueTypes = [];
    private readonly ConcurrentDictionary<int, Type> _keyRegistry = [];
    private readonly TypedArrayBucket<object?> _referenceTypes;
    private readonly Lock[] _locks;
    private readonly int _maxSize;

    /// <summary>
    /// Creates a store with one typed array per value type and a single shared array for reference types.
    /// </summary>
    /// <param name="bucketSizes">
    /// Maps each value type to its array capacity. Any reference type entry contributes to the capacity
    /// of the shared reference array (the maximum across all reference entries).
    /// </param>
    public ArrayBucketStore(Dictionary<Type, int> bucketSizes) {
        var referenceSize = 0;

        foreach (var (type, size) in bucketSizes) {
            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(bucketSizes), size, "Bucket size cannot be negative.");

            if (type.IsValueType) {
                _valueTypes[type] = Activator.CreateInstance(typeof(TypedArrayBucket<>).MakeGenericType(type), size) as IArrayBucket
                    ?? throw new InvalidOperationException("Failed to create bucket for type " + type + ".");
            } else {
                referenceSize = Math.Max(referenceSize, size);
            }
        }

        _referenceTypes = new TypedArrayBucket<object?>(referenceSize);
        _maxSize = bucketSizes.Count > 0 ? bucketSizes.Values.Max() : 0;
        _locks = new Lock[_maxSize];
        for (var i = 0; i < _maxSize; i++) {
            _locks[i] = new Lock();
        }
    }

    private int SizeOf<T>() {
        return typeof(T).IsValueType ? GetBucket<T>().Size : _referenceTypes.Size;
    }

    private TypedArrayBucket<T> GetBucket<T>() {
        return _valueTypes.GetValueOrDefault(typeof(T)) as TypedArrayBucket<T>
            ?? throw new InvalidOperationException("No bucket registered for type " + typeof(T) + ".");
    }

    private void Register(int key, Type type) {
        if (_keyRegistry.TryGetValue(key, out var oldType) && oldType != type) {
            if (oldType.IsValueType) {
                if (_valueTypes.TryGetValue(oldType, out var oldBucket)) {
                    oldBucket.Clear(key);
                }
            } else {
                _referenceTypes.Set(key, null);
            }
        }
        _keyRegistry[key] = type;
    }

    /// <summary>Stores <paramref name="value"/> at <paramref name="key"/> in the bucket for <typeparamref name="T"/>.</summary>
    public void Set<T>(int key, T value) {
        var size = SizeOf<T>();
        if (key < 0 || key >= size) {
            throw new ArgumentOutOfRangeException(nameof(key), key,
                $"Key must be within [0, {size}) for type {typeof(T)}.");
        }

        lock (_locks[key]) {
            Register(key, typeof(T));

            if (typeof(T).IsValueType) {
                GetBucket<T>().Set(key, value);
            } else {
                _referenceTypes.Set(key, value);
            }
        }
    }

    /// <summary>Stores <paramref name="value"/> at <paramref name="key"/> using <paramref name="type"/> as the compile-time type.</summary>
    public void Set(int key, object? value, Type type) {
        var method = typeof(ArrayBucketStore)
            .GetMethods()
            .First(m => m.Name == nameof(Set)
                && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 2)
            .MakeGenericMethod(type);

        method.Invoke(this, [key, value]);
    }

    /// <summary>Returns the value for <paramref name="key"/> from the bucket for <typeparamref name="T"/>, or <c>default</c>.</summary>
    public T? Get<T>(int key) {
        var size = SizeOf<T>();
        if (key < 0 || key >= size) {
            throw new ArgumentOutOfRangeException(nameof(key), key,
                $"Key must be within [0, {size}) for type {typeof(T)}.");
        }

        lock (_locks[key]) {
            if (!typeof(T).IsValueType) {
                return _referenceTypes.Get(key) is T typed ? typed : default;
            }
            return GetBucket<T>().Get(key);
        }
    }

    /// <summary>Attempts to retrieve the typed value for <paramref name="key"/>.</summary>
    public bool TryGet<T>(int key, out T? value) {
        if (typeof(T).IsValueType && !_valueTypes.ContainsKey(typeof(T))) {
            value = default;
            return false;
        }

        var size = SizeOf<T>();
        if (key < 0 || key >= size) {
            value = default;
            return false;
        }

        lock (_locks[key]) {
            if (!_keyRegistry.TryGetValue(key, out var registered) || registered != typeof(T)) {
                value = default;
                return false;
            }

            if (!typeof(T).IsValueType) {
                if (_referenceTypes.Get(key) is T typed) {
                    value = typed;
                    return true;
                }
                value = default;
                return false;
            }
            value = GetBucket<T>().Get(key);
            return true;
        }
    }

    /// <summary>Returns the value for <paramref name="key"/> boxed, or <c>null</c> if not present.</summary>
    public object? GetAny(int key) {
        if (key < 0 || key >= _maxSize) return null;

        lock (_locks[key]) {
            if (!_keyRegistry.TryGetValue(key, out var type)) return null;
            if (!type.IsValueType) return _referenceTypes.Get(key);
            return _valueTypes.TryGetValue(type, out var bucket) ? bucket.GetAny(key) : null;
        }
    }

    /// <summary>Attempts to retrieve the value for <paramref name="key"/> without knowing its compile-time type.</summary>
    public bool TryGetAny(int key, out object? value) {
        if (key < 0 || key >= _maxSize) {
            value = null;
            return false;
        }

        lock (_locks[key]) {
            if (!_keyRegistry.TryGetValue(key, out var type)) {
                value = null;
                return false;
            }
            if (!type.IsValueType) {
                value = _referenceTypes.Get(key);
                return true;
            }
            if (_valueTypes.TryGetValue(type, out var bucket)) {
                return bucket.TryGetAny(key, out value);
            }
            value = null;
            return false;
        }
    }

    /// <summary>Returns <c>true</c> if a value of exactly type <typeparamref name="T"/> was written at <paramref name="key"/>.</summary>
    public bool Has<T>(int key) {
        if (typeof(T).IsValueType && !_valueTypes.ContainsKey(typeof(T))) return false;

        var size = SizeOf<T>();
        if (key < 0 || key >= size) return false;

        lock (_locks[key]) {
            return _keyRegistry.TryGetValue(key, out var type) && type == typeof(T);
        }
    }

    /// <summary>Returns <c>true</c> if any value was written at <paramref name="key"/>.</summary>
    public bool HasAny(int key) {
        if (key < 0 || key >= _maxSize) return false;

        lock (_locks[key]) {
            return _keyRegistry.ContainsKey(key);
        }
    }

    /// <summary>Returns <c>true</c> if the value at <paramref name="key"/> is assignable to <typeparamref name="T"/>.</summary>
    public bool CanCast<T>(int key) {
        if (key < 0 || key >= _maxSize) return false;

        lock (_locks[key]) {
            return _keyRegistry.TryGetValue(key, out var type) && type.IsAssignableTo(typeof(T));
        }
    }

    /// <summary>Returns the type written at <paramref name="key"/>, or <c>null</c> if none.</summary>
    public Type? TypeOf(int key) {
        if (key < 0 || key >= _maxSize) return null;

        lock (_locks[key]) {
            return _keyRegistry.GetValueOrDefault(key);
        }
    }
}
