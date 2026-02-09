using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Shiron.Lib.Collections;

namespace Shiron.Lib.Benchmarks.Collections;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput)]
public class RingBufferBenchmark {
    private RingBuffer _buffer = null!;
    private Random _rng = null!;

    [Params(64, 256, 4096)]
    public int Capacity;

    [GlobalSetup]
    public void Setup() {
        _rng = new Random(42);
        _buffer = new RingBuffer(Capacity);

        for (var i = 0; i < Capacity; i++) _buffer.Add(_rng.NextDouble());
    }

    // Should ideally be O(1)
    [Benchmark]
    public void Add_Single() {
        _buffer.Add(1.2345);
    }

    // Should ideally be O(1)
    [Benchmark]
    public double GetAverage() {
        return _buffer.GetAverage();
    }

    // Should ideally be O(N log N)
    [Benchmark]
    public double GetMedian() {
        return _buffer.GetMedian();
    }

    // Should ideally be O(N log N)
    [Benchmark]
    public double GetAverageLow1Percent() {
        return _buffer.GetAverageLowPercentile(0.01);
    }

    // Should ideally be O(1)
    [Benchmark]
    public double GetStandardDeviation() {
        return _buffer.GetStandardDeviation();
    }
}
