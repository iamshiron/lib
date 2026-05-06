namespace Shiron.Lib.Collections.Bucket;

public class TypedBucket<TK, T> : IBucket<TK> where TK : IEquatable<TK> {
    private readonly Dictionary<TK, T> _data = [];
    public ICollection<TK> Keys => _data.Keys;
    public IReadOnlyDictionary<TK, T> ToDictionary => _data;

    public void Set(TK key, T value) {
        _data[key] = value;
    }

    public T? Get(TK key) {
        return _data.GetValueOrDefault(key);
    }

    public bool TryGet(TK key, out T value) {
        return _data.TryGetValue(key, out value!);
    }

    public bool TryGetAny(TK key, out object? value) {
        if (_data.TryGetValue(key, out var typedValue)) {
            value = typedValue;
            return true;
        }

        value = null;
        return false;
    }

    public object? GetAny(TK key) {
        return _data.GetValueOrDefault(key);
    }

    public bool Remove(TK key) {
        return _data.Remove(key);
    }

    public void Clear() {
        _data.Clear();
    }

    public bool Has(TK key) {
        return _data.ContainsKey(key);
    }
}
