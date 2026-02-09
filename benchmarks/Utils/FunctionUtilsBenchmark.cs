using System.Reflection;
using BenchmarkDotNet.Attributes;
using Shiron.Lib.Utils;

namespace Shiron.Lib.Benchmarks.Utils;

[MemoryDiagnoser]
public class FunctionUtilsBenchmarks {
    private TestClass _instance = null!;
    private MethodInfo _methodInfo = null!;
    private Func<int, int, int> _generatedDelegate = null!;
    private readonly object[] _args = [10, 20];

    [GlobalSetup]
    public void Setup() {
        _instance = new TestClass();
        _methodInfo = typeof(TestClass).GetMethod(nameof(TestClass.Add))!;

        var del = FunctionUtils.ToDelegate(_instance, _methodInfo);
        _generatedDelegate = (Func<int, int, int>) del;
    }

    [Benchmark(Baseline = true)]
    public int Reflection_Invoke() {
        return (int) _methodInfo.Invoke(_instance, _args)!;
    }

    [Benchmark]
    public int Delegate_Invoke() {
        return _generatedDelegate(10, 20);
    }

    private class TestClass {
        public int Add(int a, int b) {
            return a + b;
        }
    }
}
