namespace Shiron.Lib.Collections.Bucket;

public class TypedArrayBucket<T>(int size) : IArrayBucket {
    private readonly T?[] _data = new T?[size];

    public void Set(int key, T? value) {
        _data[key] = value;
    }
    public T? Get(int key) {
        return _data[key];
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
    public bool TryGetValue(int index, out object? value) {
        value = Get(index);
        return value != null;
    }
    public void Clear(int index) {
        _data[index] = default;
    }
    public int Size => size;
}
