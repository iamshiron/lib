namespace Shiron.Lib.Collections.Bucket;

public class TypedArrayBucket<T>(int size) : IArrayBucket {
    private readonly T?[] _data = new T?[size];

    public void Set(int index, T? value) {
        if (index < 0 || index >= size) throw new ArgumentOutOfRangeException(nameof(index));
        _data[index] = value;
    }
    public T? Get(int index) {
        if (index < 0 || index >= size) throw new ArgumentOutOfRangeException(nameof(index));
        return _data[index];
    }

    /// <inheritdoc/>
    public bool TryGetAny(int index, out object? value) {
        value = Get(index);
        return value != null;
    }
    /// <inheritdoc/>
    public object? GetAny(int index) {
        return Get(index);
    }
    public void Clear(int index) {
        if (index < 0 || index >= size) throw new ArgumentOutOfRangeException(nameof(index));
        _data[index] = default;
    }
    public int Size => size;
}
