namespace Shiron.Lib.Collections.Bucket;

/// <summary>
/// A typed, pre-allocated key-value store backed by one array per registered value type plus a
/// shared array for all reference types. Keys are integers in <c>[0, size)</c> per type.
/// </summary>
/// <remarks>
/// Presence is tracked via a flat <see cref="bool"/> array shared across all types — no per-key
/// type registry is maintained. Callers that need type information (e.g. for untyped reads via
/// <see cref="GetAny"/>) must supply the <see cref="Type"/> explicitly.
/// </remarks>
public sealed class ArrayBucketStore {
    private readonly Dictionary<Type, IArrayBucket> _valueTypes = [];
    private readonly TypedArrayBucket<object?> _referenceTypes;
    private readonly bool[] _present;
    private readonly Lock[] _locks;
    private readonly int _maxSize;

    /// <summary>
    /// Creates a store with one typed array per value type and a single shared array for reference types.
    /// </summary>
    /// <param name="bucketSizes">
    /// Maps each value type to its array capacity. Reference-type entries contribute to the capacity
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
        _present = new bool[_maxSize];
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

    /// <summary>Stores <paramref name="value"/> at <paramref name="key"/> in the bucket for <typeparamref name="T"/>.</summary>
    public void Set<T>(int key, T value) {
        var size = SizeOf<T>();
        if (key < 0 || key >= size) {
            throw new ArgumentOutOfRangeException(nameof(key), key,
                $"Key must be within [0, {size}) for type {typeof(T)}.");
        }

        lock (_locks[key]) {
            _present[key] = true;

            if (typeof(T).IsValueType) {
                GetBucket<T>().Set(key, value);
            } else {
                _referenceTypes.Set(key, value);
            }
        }
    }

    /// <summary>Stores <paramref name="value"/> at <paramref name="key"/> using <paramref name="type"/> as the routing type.</summary>
    public void Set(int key, object? value, Type type) {
        var size = type.IsValueType
            ? (_valueTypes.TryGetValue(type, out var b) ? b.Size : 0)
            : _referenceTypes.Size;

        if (key < 0 || key >= size) {
            throw new ArgumentOutOfRangeException(nameof(key), key,
                $"Key must be within [0, {size}) for type {type}.");
        }

        lock (_locks[key]) {
            _present[key] = true;

            if (type.IsValueType) {
                if (_valueTypes.TryGetValue(type, out var bucket)) {
                    bucket.SetAny(key, value);
                } else {
                    throw new InvalidOperationException("No bucket registered for type " + type + ".");
                }
            } else {
                _referenceTypes.Set(key, value);
            }
        }
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

    /// <summary>Returns <c>true</c> if any value was written at <paramref name="key"/>.</summary>
    public bool HasAny(int key) {
        if (key < 0 || key >= _maxSize) return false;
        return Volatile.Read(ref _present[key]);
    }

    /// <summary>
    /// Returns the boxed value for <paramref name="key"/>, routed by <paramref name="type"/>,
    /// or <c>null</c> if not present.
    /// </summary>
    public object? GetAny(int key, Type type) {
        if (key < 0 || key >= _maxSize || !Volatile.Read(ref _present[key])) return null;

        lock (_locks[key]) {
            if (!_present[key]) return null;

            if (type.IsValueType) {
                return _valueTypes.TryGetValue(type, out var bucket) ? bucket.GetAny(key) : null;
            }

            return _referenceTypes.Get(key);
        }
    }

    /// <summary>Clears the value at <paramref name="key"/> across all buckets and resets presence.</summary>
    public void Clear(int key) {
        if (key < 0 || key >= _maxSize) return;

        lock (_locks[key]) {
            _present[key] = false;
            foreach (var bucket in _valueTypes.Values) {
                bucket.Clear(key);
            }
            _referenceTypes.Clear(key);
        }
    }
}
