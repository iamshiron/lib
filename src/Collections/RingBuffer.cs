using System.Buffers;
using System.Runtime.CompilerServices;

namespace Shiron.Lib.Collections;

/// <summary>
/// Represents a circular buffer optimized for fixed-size storage and efficient insertion.
/// </summary>
public sealed class RingBuffer {
    private readonly double[] _buffer;
    private readonly int _mask;
    private int _count;
    private double _currentSum;
    private double _currentSumSquared;
    private int _head;

    public RingBuffer(int capacity) {
        if ((capacity & capacity - 1) != 0)
            throw new ArgumentException("Capacity must be a power of two.");

        _buffer = new double[capacity];
        _mask = capacity - 1;
        _head = 0;
        _count = 0;
        _currentSum = 0;
        _currentSumSquared = 0;
    }

    public int Capacity => _buffer.Length;
    public int Count => _count;

    /// <summary>
    /// Adds an item to the circular buffer. If the buffer is at full capacity, the oldest element will be overwritten.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(double item) {
        // If full, subtract the value we are about to overwrite from the running sum
        if (_count == _buffer.Length)
        {
            _currentSum -= _buffer[_head];
            _currentSumSquared -= _buffer[_head] * _buffer[_head];
        }

        _buffer[_head] = item;
        _currentSum += item;
        _currentSumSquared += item * item;

        _head = _head + 1 & _mask;
        if (_count < _buffer.Length) _count++;
    }

    /// <summary>
    /// Calculates the average of the buffer in O(1).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetAverage() {
        if (_count == 0) return 0;
        return _currentSum / _count;
    }
    /// <summary>
    /// Calculates the median value of the elements in the circular buffer.
    /// If the buffer contains an even number of elements, the lower middle value is returned.
    /// </summary>
    /// <returns>
    /// The median value of the buffer elements. Returns 0 if the buffer is empty.
    /// </returns>
    public double GetMedian() {
        var temp = ArrayPool<double>.Shared.Rent(_count);
        try
        {
            FillAndSort(temp);
            return temp[_count / 2];
        } finally
        {
            ArrayPool<double>.Shared.Return(temp);
        }
    }

    /// <summary>
    /// Calculates the average of the lowest elements (e.g. 1% lows).
    /// </summary>
    public double GetAverageLowPercentile(double percentile) {
        if (_count == 0) return 0;

        var sampleCount = Math.Max(1, (int) Math.Ceiling(_count * percentile));
        var temp = ArrayPool<double>.Shared.Rent(_count);
        try
        {
            FillAndSort(temp);

            double sum = 0;
            for (var i = 0; i < sampleCount; i++) sum += temp[i];

            return sum / sampleCount;
        } finally
        {
            ArrayPool<double>.Shared.Return(temp);
        }
    }

    /// <summary>
    /// Calculates the median of the lowest percentile.
    /// </summary>
    public double GetMedianLowPercentile(double percentile) {
        if (_count == 0) return 0;

        var sampleCount = Math.Max(1, (int) Math.Ceiling(_count * percentile));
        var temp = ArrayPool<double>.Shared.Rent(_count);
        try
        {
            FillAndSort(temp);

            var mid = sampleCount / 2;
            if (sampleCount % 2 != 0) return temp[mid];

            return (temp[mid - 1] + temp[mid]) / 2.0;
        } finally
        {
            ArrayPool<double>.Shared.Return(temp);
        }
    }

    /// <summary>
    /// Calculates the average of the highest elements (e.g. 1% highs).
    /// </summary>
    public double GetAverageHighPercentile(double percentile) {
        if (_count == 0) return 0;

        var sampleCount = Math.Max(1, (int) Math.Ceiling(_count * percentile));
        var temp = ArrayPool<double>.Shared.Rent(_count);
        try
        {
            FillAndSort(temp);

            double sum = 0;
            for (var i = _count - sampleCount; i < _count; i++)
                sum += temp[i];

            return sum / sampleCount;
        } finally
        {
            ArrayPool<double>.Shared.Return(temp);
        }
    }

    /// <summary>
    /// Calculates the median of the highest percentile.
    /// </summary>
    public double GetMedianHighPercentile(double percentile) {
        if (_count == 0) return 0;

        var sampleCount = Math.Max(1, (int) Math.Ceiling(_count * percentile));
        var temp = ArrayPool<double>.Shared.Rent(_count);
        try
        {
            FillAndSort(temp);

            var startIndex = _count - sampleCount;
            var midOffset = sampleCount / 2;

            if (sampleCount % 2 != 0)
                return temp[startIndex + midOffset];

            return (temp[startIndex + midOffset - 1] +
                    temp[startIndex + midOffset]) / 2.0;
        } finally
        {
            ArrayPool<double>.Shared.Return(temp);
        }
    }

    /// <summary>
    /// Calculates the variance of the elements in the circular buffer in O(1).
    /// Variance measures the dispersion of the buffer elements from their mean.
    /// </summary>
    /// <returns>
    /// The variance of the buffer elements. Returns 0 if the buffer is empty.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetVariance() {
        if (_count == 0) return 0;

        var mean = _currentSum / _count;
        var meanOfSquares = _currentSumSquared / _count;
        return Math.Max(0, meanOfSquares - mean * mean);
    }

    /// <summary>
    /// Calculates the standard deviation of the elements in the circular buffer in O(1).
    /// The standard deviation is derived as the square root of the variance.
    /// </summary>
    /// <returns>
    /// The standard deviation of the buffer elements. Returns 0 if the buffer is empty.
    /// </returns>
    public double GetStandardDeviation() {
        return Math.Sqrt(GetVariance());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FillAndSort(double[] destination) {
        _buffer.AsSpan(0, _count).CopyTo(destination);
        Array.Sort(destination, 0, _count);
    }
}
