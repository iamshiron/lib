# Benchmarks
## Shiron.Lib.Flow
### Latched Throttler
| Method                        | IntervalMS | Mean          | Error     | StdDev    | Median        | Allocated |
|------------------------------ |----------- |--------------:|----------:|----------:|--------------:|----------:|
| **Trigger**                       | **10**         |     **0.0003 ns** | **0.0003 ns** | **0.0003 ns** |     **0.0003 ns** |         **-** |
| Update_WithoutTrigger         | 10         |     9.9448 ns | 0.0011 ns | 0.0010 ns |     9.9448 ns |         - |
| Update_WithTrigger_NotElapsed | 10         |    20.3754 ns | 0.0074 ns | 0.0070 ns |    20.3740 ns |         - |
| Clear                         | 10         |     0.0000 ns | 0.0000 ns | 0.0000 ns |     0.0000 ns |         - |
| Reset                         | 10         |     0.0000 ns | 0.0000 ns | 0.0000 ns |     0.0000 ns |         - |
| TriggerAndPoll_100Updates     | 10         | 1,061.3677 ns | 0.4442 ns | 0.4155 ns | 1,061.4178 ns |         - |
| RepeatedTriggers_100Times     | 10         |    24.7144 ns | 0.0150 ns | 0.0140 ns |    24.7141 ns |         - |
| **Trigger**                       | **100**        |     **0.0001 ns** | **0.0001 ns** | **0.0001 ns** |     **0.0000 ns** |         **-** |
| Update_WithoutTrigger         | 100        |     9.9459 ns | 0.0016 ns | 0.0015 ns |     9.9457 ns |         - |
| Update_WithTrigger_NotElapsed | 100        |    20.3764 ns | 0.0069 ns | 0.0064 ns |    20.3761 ns |         - |
| Clear                         | 100        |     0.0000 ns | 0.0000 ns | 0.0000 ns |     0.0000 ns |         - |
| Reset                         | 100        |     0.0000 ns | 0.0001 ns | 0.0000 ns |     0.0000 ns |         - |
| TriggerAndPoll_100Updates     | 100        | 1,060.7469 ns | 0.5598 ns | 0.5237 ns | 1,060.7058 ns |         - |
| RepeatedTriggers_100Times     | 100        |    24.7052 ns | 0.0210 ns | 0.0164 ns |    24.7095 ns |         - |
| **Trigger**                       | **1000**       |     **0.0005 ns** | **0.0004 ns** | **0.0003 ns** |     **0.0006 ns** |         **-** |
| Update_WithoutTrigger         | 1000       |     9.9471 ns | 0.0032 ns | 0.0030 ns |     9.9482 ns |         - |
| Update_WithTrigger_NotElapsed | 1000       |    20.3727 ns | 0.0080 ns | 0.0075 ns |    20.3712 ns |         - |
| Clear                         | 1000       |     0.0002 ns | 0.0002 ns | 0.0002 ns |     0.0002 ns |         - |
| Reset                         | 1000       |     0.0000 ns | 0.0000 ns | 0.0000 ns |     0.0000 ns |         - |
| TriggerAndPoll_100Updates     | 1000       | 1,063.2110 ns | 1.9686 ns | 1.8414 ns | 1,064.1387 ns |         - |
| RepeatedTriggers_100Times     | 1000       |    24.6977 ns | 0.0343 ns | 0.0321 ns |    24.7001 ns |         - |

### LeadingDebouncer
| Method                         | SilenceTimeMS | Mean        | Error    | StdDev   | Allocated |
|------------------------------- |-------------- |------------:|---------:|---------:|----------:|
| **TryExecute_FirstCall**           | **10**            |    **26.32 ns** | **0.006 ns** | **0.006 ns** |         **-** |
| TryExecute_ImmediateSecondCall | 10            |    39.12 ns | 0.008 ns | 0.007 ns |         - |
| Reset                          | 10            |    12.06 ns | 0.003 ns | 0.002 ns |         - |
| RapidFire_100Calls             | 10            | 1,357.20 ns | 0.360 ns | 0.319 ns |         - |
| TryExecute_InterlockedOverhead | 10            |    13.04 ns | 0.003 ns | 0.002 ns |         - |
| AlternatingPattern_50Cycles    | 10            | 1,328.31 ns | 0.495 ns | 0.463 ns |         - |
| **TryExecute_FirstCall**           | **100**           |    **26.33 ns** | **0.008 ns** | **0.008 ns** |         **-** |
| TryExecute_ImmediateSecondCall | 100           |    39.35 ns | 0.012 ns | 0.011 ns |         - |
| Reset                          | 100           |    12.06 ns | 0.004 ns | 0.003 ns |         - |
| RapidFire_100Calls             | 100           | 1,357.82 ns | 0.522 ns | 0.463 ns |         - |
| TryExecute_InterlockedOverhead | 100           |    13.04 ns | 0.003 ns | 0.002 ns |         - |
| AlternatingPattern_50Cycles    | 100           | 1,328.60 ns | 0.412 ns | 0.386 ns |         - |
| **TryExecute_FirstCall**           | **1000**          |    **26.31 ns** | **0.010 ns** | **0.010 ns** |         **-** |
| TryExecute_ImmediateSecondCall | 1000          |    38.99 ns | 0.008 ns | 0.008 ns |         - |
| Reset                          | 1000          |    12.06 ns | 0.001 ns | 0.001 ns |         - |
| RapidFire_100Calls             | 1000          | 1,358.52 ns | 0.347 ns | 0.325 ns |         - |
| TryExecute_InterlockedOverhead | 1000          |    13.04 ns | 0.002 ns | 0.002 ns |         - |
| AlternatingPattern_50Cycles    | 1000          | 1,327.90 ns | 0.441 ns | 0.412 ns |         - |

### TrailingDebouncer
| Method                                | SilenceTimeMS | Mean          | Error     | StdDev    | Allocated |
|-------------------------------------- |-------------- |--------------:|----------:|----------:|----------:|
| **Signal**                                | **10**            |     **9.9771 ns** | **0.0042 ns** | **0.0037 ns** |         **-** |
| TryResolve_NoPending                  | 10            |     0.1891 ns | 0.0002 ns | 0.0001 ns |         - |
| TryResolve_WithPending_NotElapsed     | 10            |    19.9859 ns | 0.0032 ns | 0.0030 ns |         - |
| TrailingPattern_10Signals_100Resolves | 10            | 1,155.7309 ns | 0.1490 ns | 0.1393 ns |         - |
| RapidSignaling_100Times               | 10            | 1,047.2119 ns | 0.5525 ns | 0.4898 ns |         - |
| EmptyPolling_100Times                 | 10            |    29.6034 ns | 0.1103 ns | 0.1031 ns |         - |
| AlternatingSignalResolve_50Cycles     | 10            | 1,046.2683 ns | 0.2466 ns | 0.2186 ns |         - |
| **Signal**                                | **100**           |     **9.9796 ns** | **0.0037 ns** | **0.0035 ns** |         **-** |
| TryResolve_NoPending                  | 100           |     0.1892 ns | 0.0001 ns | 0.0001 ns |         - |
| TryResolve_WithPending_NotElapsed     | 100           |    19.9862 ns | 0.0033 ns | 0.0029 ns |         - |
| TrailingPattern_10Signals_100Resolves | 100           | 1,156.2892 ns | 0.4111 ns | 0.3846 ns |         - |
| RapidSignaling_100Times               | 100           | 1,046.8632 ns | 0.0974 ns | 0.0864 ns |         - |
| EmptyPolling_100Times                 | 100           |    29.5385 ns | 0.1992 ns | 0.1863 ns |         - |
| AlternatingSignalResolve_50Cycles     | 100           | 1,046.0969 ns | 0.2830 ns | 0.2648 ns |         - |
| **Signal**                                | **1000**          |     **9.9762 ns** | **0.0055 ns** | **0.0052 ns** |         **-** |
| TryResolve_NoPending                  | 1000          |     0.1893 ns | 0.0008 ns | 0.0008 ns |         - |
| TryResolve_WithPending_NotElapsed     | 1000          |    20.0058 ns | 0.0217 ns | 0.0203 ns |         - |
| TrailingPattern_10Signals_100Resolves | 1000          | 1,156.7625 ns | 0.3416 ns | 0.3028 ns |         - |
| RapidSignaling_100Times               | 1000          | 1,046.6921 ns | 0.1681 ns | 0.1312 ns |         - |
| EmptyPolling_100Times                 | 1000          |    29.6112 ns | 0.1221 ns | 0.1142 ns |         - |
| AlternatingSignalResolve_50Cycles     | 1000          | 1,046.2290 ns | 0.3837 ns | 0.3589 ns |         - |

### Throttler
| Method                   | IntervalMS | Mean          | Error     | StdDev    | Median        | Allocated |
|------------------------- |----------- |--------------:|----------:|----------:|--------------:|----------:|
| **TryExecute_WhenAllowed**   | **10**         |    **20.1606 ns** | **0.0086 ns** | **0.0080 ns** |    **20.1614 ns** |         **-** |
| TryExecute_WhenThrottled | 10         |    31.2746 ns | 0.0121 ns | 0.0114 ns |    31.2720 ns |         - |
| CooldownProgress         | 10         |    13.9319 ns | 0.0016 ns | 0.0014 ns |    13.9318 ns |         - |
| GetTimeRemainingMS       | 10         |     9.7569 ns | 0.0024 ns | 0.0023 ns |     9.7573 ns |         - |
| GetTimeRemaining         | 10         |    10.1728 ns | 0.0046 ns | 0.0040 ns |    10.1743 ns |         - |
| Reset                    | 10         |     0.0000 ns | 0.0000 ns | 0.0000 ns |     0.0000 ns |         - |
| BurstScenario_100Calls   | 10         | 1,078.5259 ns | 0.8839 ns | 0.8268 ns | 1,078.7643 ns |         - |
| **TryExecute_WhenAllowed**   | **100**        |    **20.1685 ns** | **0.0091 ns** | **0.0085 ns** |    **20.1674 ns** |         **-** |
| TryExecute_WhenThrottled | 100        |    31.0518 ns | 0.0156 ns | 0.0138 ns |    31.0549 ns |         - |
| CooldownProgress         | 100        |    13.9327 ns | 0.0152 ns | 0.0142 ns |    13.9363 ns |         - |
| GetTimeRemainingMS       | 100        |     9.7565 ns | 0.0024 ns | 0.0023 ns |     9.7557 ns |         - |
| GetTimeRemaining         | 100        |    10.1686 ns | 0.0016 ns | 0.0014 ns |    10.1687 ns |         - |
| Reset                    | 100        |     0.0001 ns | 0.0001 ns | 0.0001 ns |     0.0000 ns |         - |
| BurstScenario_100Calls   | 100        | 1,079.0532 ns | 1.1149 ns | 1.0429 ns | 1,079.2552 ns |         - |
| **TryExecute_WhenAllowed**   | **1000**       |    **20.1661 ns** | **0.0066 ns** | **0.0062 ns** |    **20.1655 ns** |         **-** |
| TryExecute_WhenThrottled | 1000       |    30.6742 ns | 0.0530 ns | 0.0496 ns |    30.6869 ns |         - |
| CooldownProgress         | 1000       |    13.9347 ns | 0.0032 ns | 0.0030 ns |    13.9353 ns |         - |
| GetTimeRemainingMS       | 1000       |     9.7583 ns | 0.0018 ns | 0.0017 ns |     9.7584 ns |         - |
| GetTimeRemaining         | 1000       |    10.1674 ns | 0.0021 ns | 0.0018 ns |    10.1667 ns |         - |
| Reset                    | 1000       |     0.0000 ns | 0.0001 ns | 0.0001 ns |     0.0000 ns |         - |
| BurstScenario_100Calls   | 1000       | 1,079.0363 ns | 0.6536 ns | 0.5794 ns | 1,079.0846 ns |         - |


## Shiron.Lib.Collections
### RingBuffer
---
| Method                | Capacity |            Mean |       Error |      StdDev | Allocated |
|---------------------- |--------- |----------------:|------------:|------------:|----------:|
| **Add_Single** | **64** |     **2.9265 ns** |   **0.0029 ns** |   **0.0025 ns** |         **-** |
| GetAverage            | 64       |       0.0120 ns |   0.0004 ns |   0.0004 ns |         - |
| GetMedian             | 64       |     217.1687 ns |   0.2168 ns |   0.2028 ns |         - |
| GetAverageLow1Percent | 64       |     216.1391 ns |   0.3889 ns |   0.3638 ns |         - |
| GetStandardDeviation  | 64       |       1.4177 ns |   0.0009 ns |   0.0008 ns |         - |
| **Add_Single** | **256** |     **3.2097 ns** |   **0.0020 ns** |   **0.0016 ns** |         **-** |
| GetAverage            | 256      |       0.0119 ns |   0.0001 ns |   0.0001 ns |         - |
| GetMedian             | 256      |   1,124.5402 ns |   2.8827 ns |   2.2507 ns |         - |
| GetAverageLow1Percent | 256      |   1,184.7619 ns |   1.3850 ns |   1.1565 ns |         - |
| GetStandardDeviation  | 256      |       1.4180 ns |   0.0003 ns |   0.0002 ns |         - |
| **Add_Single** | **4096** |     **3.2665 ns** |   **0.0022 ns** |   **0.0021 ns** |         **-** |
| GetAverage            | 4096     |       0.0116 ns |   0.0002 ns |   0.0002 ns |         - |
| GetMedian             | 4096     | 106,071.7661 ns |   294.4376 ns | 275.4171 ns |         - |
| GetAverageLow1Percent | 4096     | 108,860.9056 ns |   195.2957 ns | 163.0807 ns |         - |
| GetStandardDeviation  | 4096     |       1.4190 ns |   0.0004 ns |   0.0003 ns |         - |

## Shiron.Lib.Logging
### Standard Logging
| Method                       | Mean      | Error    | StdDev   | Gen0   | Allocated |
|----------------------------- |----------:|---------:|---------:|-------:|----------:|
| BasicLogging_NoRenderer      |  13.61 ns | 0.006 ns | 0.005 ns |      - |         - |
| BasicLogging_WithRenderer    |  17.14 ns | 0.013 ns | 0.011 ns |      - |         - |
| JsonLogging                  | 109.75 ns | 0.143 ns | 0.134 ns | 0.0014 |      24 B |
| Logging_WithCaptureInjector  | 626.53 ns | 0.522 ns | 0.407 ns | 0.0391 |     624 B |
| Logging_WithSuppressInjector | 585.93 ns | 1.023 ns | 0.907 ns | 0.0153 |     248 B |

### Log Rendering
| Method                              | Mean      | Error     | StdDev    | Allocated |
|------------------------------------ |----------:|----------:|----------:|----------:|
| MultipleRenderers_Overhead          | 12.903 ns | 0.1929 ns | 0.1804 ns |         - |
| LogRenderUtils_WriteLogLevel        |  7.078 ns | 0.0077 ns | 0.0072 ns |         - |
| LogRenderUtils_WriteTimestamp       |  6.485 ns | 0.0056 ns | 0.0053 ns |         - |
| LogRenderUtils_WriteSpanFormattable |  5.624 ns | 0.0049 ns | 0.0046 ns |         - |

### Contextual logging
| Method                | Mean      | Error    | StdDev   | Allocated |
|---------------------- |----------:|---------:|---------:|----------:|
| RootContextCreation   |  24.95 ns | 0.010 ns | 0.009 ns |         - |
| ChildContextCreation  |  53.40 ns | 0.035 ns | 0.033 ns |         - |
| DeepContextualLogging | 127.67 ns | 0.098 ns | 0.092 ns |         - |

## Shiron.Lib.Utils
### FunctionUtils
| Method            | Mean       | Error     | StdDev    | Median     | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |-----------:|----------:|----------:|-----------:|------:|-------:|----------:|------------:|
| Reflection_Invoke | 19.2491 ns | 0.0820 ns | 0.0727 ns | 19.2215 ns | 1.000 | 0.0015 |      24 B |        1.00 |
| Delegate_Invoke   |  0.0003 ns | 0.0004 ns | 0.0004 ns |  0.0002 ns | 0.000 |      - |         - |        0.00 |

### HashUtils
| Method     | Mean     | Error   | StdDev  | Gen0   | Allocated |
|----------- |---------:|--------:|--------:|-------:|----------:|
| HashObject | 587.5 ns | 0.96 ns | 0.85 ns | 0.0591 |     928 B |

*Note*: The `HashObject` allocates memory during serialization. Use sparingly in hot paths.

## Enviornment Info:
This is where I ran all the benchmarks:
```
BenchmarkDotNet v0.15.8, Linux Arch Linux
13th Gen Intel Core i7-13700KF 0.80GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.104
  [Host]     : .NET 10.0.4 (10.0.4, 42.42.42.42424), X64 RyuJIT x86-64-v3
  Job-CEIKLR : .NET 10.0.4 (10.0.4, 42.42.42.42424), X64 RyuJIT x86-64-v3
```
