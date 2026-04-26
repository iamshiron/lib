namespace Shiron.Lib.Collections;

/// <summary>
/// Represents a read-only circular buffer for statistical computations over <see cref="double"/> values.
/// </summary>
public interface IReadOnlyRingBuffer {
    /// <summary>
    /// Gets the maximum number of elements the buffer can hold. Must be a power of two.
    /// </summary>
    internal int Capacity { get; }

    /// <summary>
    /// Gets the current number of elements in the buffer.
    /// </summary>
    internal int Count { get; }

    /// <summary>
    /// Calculates the arithmetic mean of the buffer elements in O(1).
    /// </summary>
    /// <returns>The average of the buffer elements, or 0 if the buffer is empty.</returns>
    internal double GetAverage();

    /// <summary>
    /// Calculates the median value of the buffer elements.
    /// For an even number of elements, the lower middle value is returned.
    /// </summary>
    /// <returns>The median value, or 0 if the buffer is empty.</returns>
    internal double GetMedian();

    /// <summary>
    /// Calculates the average of the lowest <paramref name="percentile"/> fraction of elements (e.g. 1% lows).
    /// </summary>
    /// <param name="percentile">The fraction of lowest elements to include, between 0 and 1.</param>
    /// <returns>The average of the lowest percentile elements, or 0 if the buffer is empty.</returns>
    internal double GetAverageLowPercentile(double percentile);

    /// <summary>
    /// Calculates the median of the lowest <paramref name="percentile"/> fraction of elements.
    /// </summary>
    /// <param name="percentile">The fraction of lowest elements to include, between 0 and 1.</param>
    /// <returns>The median of the lowest percentile elements, or 0 if the buffer is empty.</returns>
    internal double GetMedianLowPercentile(double percentile);

    /// <summary>
    /// Calculates the average of the highest <paramref name="percentile"/> fraction of elements (e.g. 1% highs).
    /// </summary>
    /// <param name="percentile">The fraction of highest elements to include, between 0 and 1.</param>
    /// <returns>The average of the highest percentile elements, or 0 if the buffer is empty.</returns>
    internal double GetAverageHighPercentile(double percentile);

    /// <summary>
    /// Calculates the median of the highest <paramref name="percentile"/> fraction of elements.
    /// </summary>
    /// <param name="percentile">The fraction of highest elements to include, between 0 and 1.</param>
    /// <returns>The median of the highest percentile elements, or 0 if the buffer is empty.</returns>
    internal double GetMedianHighPercentile(double percentile);

    /// <summary>
    /// Calculates the population variance of the buffer elements in O(1) using running sums.
    /// </summary>
    /// <returns>The variance of the buffer elements, or 0 if the buffer is empty.</returns>
    internal double GetVariance();

    /// <summary>
    /// Calculates the population standard deviation of the buffer elements in O(1).
    /// Derived as the square root of the variance.
    /// </summary>
    /// <returns>The standard deviation of the buffer elements, or 0 if the buffer is empty.</returns>
    internal double GetStandardDeviation();
}
