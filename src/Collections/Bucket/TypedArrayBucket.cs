namespace Shiron.Lib.Collections.Bucket;

/// <summary>
/// A strongly-typed fixed-capacity array bucket storing <typeparamref name="T"/> values.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public sealed class TypedArrayBucket<T>(int size) : IArrayBucket {
    private readonly T?[] _data = new T?[size];

    /// <summary>Stores <paramref name="value"/> at <paramref name="index"/>.</summary>
    public void Set(int index, T? value) {
        if (index < 0 || index >= size) throw new ArgumentOutOfRangeException(nameof(index));
        _data[index] = value;
    }

    /// <summary>Returns the value at <paramref name="index"/>, or default if unset.</summary>
    public T? Get(int index) {
        if (index < 0 || index >= size) throw new ArgumentOutOfRangeException(nameof(index));
        return _data[index];
    }

    /// <inheritdoc/>
    public void SetAny(int index, object? value) => Set(index, (T?) value);

    /// <inheritdoc/>
    public object? GetAny(int index) => Get(index);

    /// <inheritdoc/>
    public void Clear(int index) {
        if (index < 0 || index >= size) throw new ArgumentOutOfRangeException(nameof(index));
        _data[index] = default;
    }

    /// <inheritdoc/>
    public int Size => size;
}
