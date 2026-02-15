using BenchmarkDotNet.Running;

namespace Shiron.Lib.Benchmarks.Logging;

public class Program {
    public static void Main(string[] args) {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
