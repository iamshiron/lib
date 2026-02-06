# Benchmarks
## Shiron.Lib.Collections
### RingBuffer
---
| Method                | Capacity |            Mean |       Error |      StdDev |          Median | Allocated |
|---------------------- |--------- |----------------:|------------:|------------:|----------------:|----------:|
| **Add_Single** | **64** |     **1.4173 ns** |   **0.0054 ns** |   **0.0050 ns** |     **1.4157 ns** |         **-** |
| GetAverage            | 64       |       0.0001 ns |   0.0003 ns |   0.0003 ns |       0.0000 ns |         - |
| GetMedian             | 64       |     214.5673 ns |   0.4915 ns |   0.4357 ns |     214.4403 ns |         - |
| GetAverageLow1Percent | 64       |     214.3901 ns |   0.9690 ns |   0.7565 ns |     214.0069 ns |         - |
| GetStandardDeviation  | 64       |       5.2064 ns |   0.0661 ns |   0.0586 ns |       5.2080 ns |         - |
| **Add_Single** | **256** |     **4.1158 ns** |   **0.0419 ns** |   **0.0350 ns** |     **4.1113 ns** |         **-** |
| GetAverage            | 256      |       0.3268 ns |   0.0192 ns |   0.0180 ns |       0.3211 ns |         - |
| GetMedian             | 256      |   1,812.3096 ns |  15.7208 ns |  14.7052 ns |   1,811.9957 ns |         - |
| GetAverageLow1Percent | 256      |   1,749.5446 ns |  24.6818 ns |  21.8797 ns |   1,738.0277 ns |         - |
| GetStandardDeviation  | 256      |       5.1275 ns |   0.0660 ns |   0.0585 ns |       5.1385 ns |         - |
| **Add_Single** | **4096** |     **4.1213 ns** |   **0.0429 ns** |   **0.0402 ns** |     **4.1139 ns** |         **-** |
| GetAverage            | 4096     |       0.3548 ns |   0.0334 ns |   0.0312 ns |       0.3404 ns |         - |
| GetMedian             | 4096     | 139,658.3049 ns | 1,534.2152 ns | 1,435.1058 ns | 139,667.2119 ns |         - |
| GetAverageLow1Percent | 4096     | 140,840.9031 ns | 1,570.5674 ns | 1,392.2671 ns | 140,885.2417 ns |         - |
| GetStandardDeviation  | 4096     |       5.1741 ns |   0.0326 ns |   0.0289 ns |       5.1635 ns |         - |

## Shiron.Lib.Utils
### FunctionUtils
| Method            | Mean       | Error     | StdDev    | Median     | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |-----------:|----------:|----------:|-----------:|------:|-------:|----------:|------------:|
| Reflection_Invoke | 14.1328 ns | 0.0438 ns | 0.0366 ns | 14.1244 ns | 1.000 | 0.0015 |      24 B |        1.00 |
| Delegate_Invoke   |  0.0005 ns | 0.0011 ns | 0.0010 ns |  0.0000 ns | 0.000 |      - |         - |        0.00 |

### HashUtils
| Method     | Mean     | Error   | StdDev  | Gen0   | Allocated |
|----------- |---------:|--------:|--------:|-------:|----------:|
| HashObject | 466.6 ns | 2.08 ns | 1.84 ns | 0.0591 |     928 B |

*Note*: The `HashObject` allocates memory during serialization. Use sparingly in hot paths.

## Enviornment Info:
This is where I ran all the benchmarks:
```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7623/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i7-13700KF 3.40GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.102
[Host]     : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v3
Job-CEIKLR : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v3

RunStrategy=Throughput
```
