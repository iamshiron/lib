using Shiron.Lib.Collections;
using Xunit;

namespace Shiron.Lib.Tests.Collections;

public class RingBufferTests {
    [Fact]
    public void RunningSum_Drift_IsBounded_AfterManyOverwrites() {
        const int capacity = 16;
        const int totalAdds = 1_000_000;
        var rng = new Random(42);
        var buffer = new RingBuffer(capacity);
        var window = new Queue<double>();

        for (var i = 0; i < totalAdds; i++) {
            var value = rng.NextDouble() * 1e6;
            buffer.Add(value);
            window.Enqueue(value);
            if (window.Count > capacity) window.Dequeue();
        }

        var expectedSum = window.Sum();
        var actualSum = buffer.GetAverage() * buffer.Count;

        var drift = Math.Abs(actualSum - expectedSum);
        Assert.True(drift < 1e-6, $"Sum drift too large: {drift} (expected {expectedSum}, got {actualSum})");
    }

    [Fact]
    public void RunningSumSquared_Drift_IsBounded_AfterManyOverwrites() {
        const int capacity = 16;
        const int totalAdds = 1_000_000;
        var rng = new Random(42);
        var buffer = new RingBuffer(capacity);
        var window = new Queue<double>();

        for (var i = 0; i < totalAdds; i++) {
            var value = rng.NextDouble() * 1e6;
            buffer.Add(value);
            window.Enqueue(value);
            if (window.Count > capacity) window.Dequeue();
        }

        var expectedVariance = window.Sum(v => v * v) / window.Count - Math.Pow(window.Average(), 2);
        var actualVariance = buffer.GetVariance();

        var relativeDrift = Math.Abs((actualVariance - expectedVariance) / expectedVariance);
        Assert.True(relativeDrift < 1e-10, $"Relative variance drift too large: {relativeDrift} (expected {expectedVariance}, got {actualVariance})");
    }

    [Fact]
    public void HighPrecision_SumDrift_AgainstDecimalBaseline() {
        const int capacity = 16384;
        const int totalAdds = 20_000_000;
        var rng = new Random(99);
        var buffer = new RingBuffer(capacity);
        var window = new Queue<decimal>();

        for (var i = 0; i < totalAdds; i++) {
            var magnitude = Math.Pow(10, rng.Next(-6, 10));
            var sign = rng.Next(2) == 0 ? 1.0 : -1.0;
            var value = sign * rng.NextDouble() * magnitude;
            buffer.Add(value);
            window.Enqueue((decimal) value);
            if (window.Count > capacity) window.Dequeue();
        }

        var expectedSumDecimal = window.Sum();
        var actualSum = buffer.GetAverage() * buffer.Count;
        var expectedSumDouble = (double) expectedSumDecimal;

        var drift = Math.Abs(actualSum - expectedSumDouble);
        var relativeDrift = drift / Math.Abs(expectedSumDouble);
        Assert.True(relativeDrift < 1e-14,
            $"Sum drift from decimal baseline too large: relative={relativeDrift}, " +
            $"absolute={drift} (expected ~{expectedSumDecimal}, got {actualSum})");
    }

    [Fact]
    public void HighPrecision_VarianceDrift_AgainstDecimalBaseline() {
        const int capacity = 8192;
        const int totalAdds = 5_000_000;
        var rng = new Random(77);
        var buffer = new RingBuffer(capacity);
        var window = new Queue<decimal>();

        for (var i = 0; i < totalAdds; i++) {
            var value = rng.NextDouble() * 1e6;
            buffer.Add(value);
            window.Enqueue((decimal) value);
            if (window.Count > capacity) window.Dequeue();
        }

        var decimalMean = window.Average();
        var decimalVariance = window.Average(v => (v - decimalMean) * (v - decimalMean));
        var expectedVariance = (double) decimalVariance;
        var actualVariance = buffer.GetVariance();

        var relativeDrift = Math.Abs((actualVariance - expectedVariance) / expectedVariance);
        Assert.True(relativeDrift < 1e-14,
            $"Variance drift from decimal baseline too large: relative={relativeDrift}, " +
            $"expected={expectedVariance}, got={actualVariance}");
    }

    [Fact]
    public void HighPrecision_CatastrophicCancellation_SumDrift() {
        const int capacity = 4096;
        const int totalAdds = 5_000_000;
        var rng = new Random(55);
        var buffer = new RingBuffer(capacity);
        var window = new Queue<decimal>();

        for (var i = 0; i < totalAdds; i++) {
            var magnitude = Math.Pow(10, rng.Next(-8, 9));
            var sign = rng.Next(2) == 0 ? 1.0 : -1.0;
            var value = sign * rng.NextDouble() * magnitude;
            buffer.Add(value);
            window.Enqueue((decimal) value);
            if (window.Count > capacity) window.Dequeue();
        }

        var expectedSumDecimal = window.Sum();
        var actualSum = buffer.GetAverage() * buffer.Count;
        var expectedSumDouble = (double) expectedSumDecimal;

        var absoluteDrift = Math.Abs(actualSum - expectedSumDouble);
        Assert.True(absoluteDrift < 1e-6,
            $"Catastrophic cancellation drift too large: absolute={absoluteDrift}, " +
            $"expected ~{expectedSumDecimal}, got {actualSum}");
    }

    [Fact]
    public void RunningSum_Drift_WithMixedMagnitudes() {
        const int capacity = 64;
        const int totalAdds = 500_000;
        var rng = new Random(123);
        var buffer = new RingBuffer(capacity);
        var window = new Queue<double>();

        for (var i = 0; i < totalAdds; i++) {
            double value = i % 3 switch {
                0 => rng.NextDouble() * 1e-10,
                1 => rng.NextDouble() * 1e10,
                _ => rng.NextDouble(),
            };
            buffer.Add(value);
            window.Enqueue(value);
            if (window.Count > capacity) window.Dequeue();
        }

        var expectedSum = window.Sum();
        var actualSum = buffer.GetAverage() * buffer.Count;

        var relativeDrift = Math.Abs((actualSum - expectedSum) / expectedSum);
        Assert.True(relativeDrift < 1e-10, $"Relative sum drift too large: {relativeDrift}");
    }
}
