namespace Shiron.Lib.Collections;

/// <summary>
/// Represents a mutable circular buffer for statistical computations over <see cref="double"/> values.
/// </summary>
public interface IRingBuffer : IReadOnlyRingBuffer {
    /// <summary>
    /// Adds an item to the circular buffer. If the buffer is at full capacity, the oldest element is overwritten.
    /// </summary>
    /// <param name="item">The value to add.</param>
    void Add(double item);

    /// <summary>
    /// Recalculates the running sums from scratch to correct floating-point drift.
    /// Called automatically when the buffer wraps around.
    /// </summary>
    void SyncSums();
}
