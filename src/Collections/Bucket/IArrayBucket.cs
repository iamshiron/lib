namespace Shiron.Lib.Collections.Bucket;

/// <summary>
/// Type-erased interface for a single typed array bucket inside an <see cref="ArrayBucketStore"/>.
/// </summary>
public interface IArrayBucket {
    /// <summary>Stores <paramref name="value"/> (boxed) at <paramref name="index"/>.</summary>
    void SetAny(int index, object? value);

    /// <summary>Returns the value for <paramref name="index"/> (boxed), or default if unset.</summary>
    object? GetAny(int index);

    /// <summary>Resets <paramref name="index"/> to the default for the bucket's type.</summary>
    void Clear(int index);

    /// <summary>Capacity of this bucket.</summary>
    int Size { get; }
}
