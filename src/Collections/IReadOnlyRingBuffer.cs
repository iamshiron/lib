namespace Shiron.Lib.Collections;

public interface IReadOnlyRingBuffer {
    internal int Capacity { get; }
    internal int Count { get; }
    internal double GetAverage();
    internal double GetMedian();
    internal double GetAverageLowPercentile(double percentile);
    internal double GetMedianLowPercentile(double percentile);
    internal double GetAverageHighPercentile(double percentile);
    internal double GetMedianHighPercentile(double percentile);
    internal double GetVariance();
    internal double GetStandardDeviation();
}
