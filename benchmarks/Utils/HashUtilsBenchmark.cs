using BenchmarkDotNet.Attributes;
using Shiron.Lib.Utils;

namespace Shiron.Lib.Benchmarks.Utils;

[MemoryDiagnoser]
public class HashUtilsBenchmarks {
    private object _complexObject = null!;

    [GlobalSetup]
    public void Setup() {
        _complexObject = new {
            Id = 1,
            Name = "PlayerConfig",
            Graphics = new { Width = 1920, Height = 1080, VSync = true },
            Tags = new[] { "Render", "Physics", "Audio" }
        };
    }

    [Benchmark]
    public string HashObject() {
        return HashUtils.HashObject(_complexObject);
    }
}
