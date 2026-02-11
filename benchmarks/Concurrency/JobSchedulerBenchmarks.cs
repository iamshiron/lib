using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Shiron.Lib.Concurrency;

namespace Shiron.Lib.Benchmarks.Concurrency;

[MemoryDiagnoser]
[ShortRunJob]
public class JobSchedulerBenchmarks {
    private JobScheduler _scheduler = null!;
    private const int ItemCount = 10_000;

    private int[] _data = null!;

    [GlobalSetup]
    public void Setup() {
        _scheduler = new JobScheduler(4);
        _data = new int[ItemCount];
    }

    [GlobalCleanup]
    public void Cleanup() {
        _scheduler.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void Schedule_Struct_FireAndForget() {
        for (var i = 0; i < ItemCount; i++) _scheduler.Schedule(new IncrementJob(i, _data));
    }

    [Benchmark]
    public void Schedule_Lambda_FireAndForget() {
        for (var i = 0; i < ItemCount; i++) {
            var index = i;
            _scheduler.RunAsync(() => _data[index]++);
        }
    }

    [Benchmark]
    public void Schedule_Parallel_Batch() {
        _scheduler.ScheduleParallel(new ParallelUpdateJob(_data), ItemCount, 64);
    }
}

public readonly struct IncrementJob(int index, int[] buffer) : IJob {
    public void Execute() {
        buffer[index]++;
    }
}

public readonly struct ParallelUpdateJob(int[] buffer) : IParallelJob {
    public void Execute(int index) {
        buffer[index]++;
    }
}
