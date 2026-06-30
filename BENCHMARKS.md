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

## Shiron.Lib.Pipeline
### DAG
| Method          | Size | Mean        | Error     | StdDev    | Gen0    | Gen1    | Allocated |
|---------------- |----- |------------:|----------:|----------:|--------:|--------:|----------:|
| **AddNodes**        | **10**   |    **284.4 ns** |   **0.80 ns** |   **0.74 ns** |  **0.2098** |  **0.0019** |   **3.22 KB** |
| AddEdges_Linear | 10   |    784.9 ns |   1.55 ns |   1.37 ns |  0.4663 |  0.0076 |   7.16 KB |
| AddEdges_Skip   | 10   |    722.4 ns |   0.90 ns |   0.85 ns |  0.4387 |  0.0067 |   6.72 KB |
| TopologicalSort | 10   |    329.7 ns |   0.43 ns |   0.40 ns |  0.0944 |       - |   1.45 KB |
| ToLayers        | 10   |    639.7 ns |   0.90 ns |   0.80 ns |  0.1745 |       - |   2.68 KB |
| RemoveEdges     | 10   |    859.9 ns |   2.22 ns |   1.85 ns |  0.4663 |  0.0076 |   7.16 KB |
| RemoveNodes     | 10   |    948.4 ns |   7.74 ns |   7.24 ns |  0.4663 |  0.0067 |   7.16 KB |
| **AddNodes**        | **100**  |  **2,358.5 ns** |   **3.79 ns** |   **3.54 ns** |  **2.1172** |  **0.1907** |  **32.44 KB** |
| AddEdges_Linear | 100  |  7,373.4 ns |  15.30 ns |  11.94 ns |  4.9438 |  0.8087 |  75.75 KB |
| AddEdges_Skip   | 100  |  7,488.3 ns |  27.09 ns |  22.63 ns |  4.9133 |  0.8163 |  75.31 KB |
| TopologicalSort | 100  |  3,148.2 ns |   5.35 ns |   5.00 ns |  0.8278 |  0.0114 |  12.73 KB |
| ToLayers        | 100  |  5,050.8 ns |  12.76 ns |  10.66 ns |  1.5869 |  0.0458 |  24.32 KB |
| RemoveEdges     | 100  |  8,440.0 ns |  41.45 ns |  36.74 ns |  4.9438 |  0.8087 |  75.75 KB |
| RemoveNodes     | 100  |  9,255.2 ns |  11.50 ns |  10.76 ns |  4.9438 |  0.6714 |  75.75 KB |
| **AddNodes**        | **500**  | **10,733.2 ns** |  **57.28 ns** |  **53.58 ns** | **10.1929** |  **3.3875** | **156.47 KB** |
| AddEdges_Linear | 500  | 38,310.5 ns | 762.35 ns | 782.88 ns | 24.4141 | 12.1460 | 374.78 KB |
| AddEdges_Skip   | 500  | 37,274.4 ns |  86.10 ns |  76.32 ns | 24.4141 | 12.1460 | 374.34 KB |
| TopologicalSort | 500  | 15,196.3 ns |  15.74 ns |  12.29 ns |  3.8757 |  0.2136 |  59.43 KB |
| ToLayers        | 500  | 24,664.3 ns | 278.21 ns | 232.31 ns |  7.5684 |  0.9460 | 116.21 KB |
| RemoveEdges     | 500  | 42,602.1 ns |  36.33 ns |  30.34 ns | 24.4141 | 12.1460 | 374.78 KB |
| RemoveNodes     | 500  | 46,436.3 ns | 100.52 ns |  83.94 ns | 24.4141 | 12.1460 | 374.78 KB |

### Pipeline Builder
| Method             | Size | Mean       | Error     | StdDev    | Gen0    | Gen1   | Allocated |
|------------------- |----- |-----------:|----------:|----------:|--------:|-------:|----------:|
| **Build_LinearChain**  | **5**    |  **29.433 μs** | **0.2187 μs** | **0.2045 μs** |  **3.0823** | **0.2747** |  **47.52 KB** |
| Build_WideFanOut   | 5    |  30.213 μs | 0.0295 μs | 0.0276 μs |  3.0823 | 0.2441 |  47.44 KB |
| Build_Diamond      | 5    |   8.684 μs | 0.0090 μs | 0.0079 μs |  0.8698 | 0.0153 |  13.41 KB |
| Build_ComplexGraph | 5    |  44.210 μs | 0.0476 μs | 0.0422 μs |  4.5166 | 0.4883 |  69.18 KB |
| **Build_LinearChain**  | **10**   |  **57.295 μs** | **0.0435 μs** | **0.0386 μs** |  **6.4087** | **1.0376** |   **98.2 KB** |
| Build_WideFanOut   | 10   |  59.003 μs | 0.0530 μs | 0.0469 μs |  6.3477 | 1.0376 |  97.51 KB |
| Build_Diamond      | 10   |   8.627 μs | 0.0032 μs | 0.0025 μs |  0.8698 | 0.0153 |  13.41 KB |
| Build_ComplexGraph | 10   |  88.580 μs | 0.1687 μs | 0.1495 μs |  9.1553 | 1.7090 | 141.51 KB |
| **Build_LinearChain**  | **25**   | **145.929 μs** | **0.4317 μs** | **0.3605 μs** | **15.3809** | **4.8828** | **237.91 KB** |
| Build_WideFanOut   | 25   | 146.578 μs | 0.1304 μs | 0.1156 μs | 15.1367 | 4.6387 | 232.25 KB |
| Build_Diamond      | 25   |   8.789 μs | 0.0117 μs | 0.0091 μs |  0.8698 | 0.0153 |  13.41 KB |
| Build_ComplexGraph | 25   | 220.684 μs | 0.2250 μs | 0.1995 μs | 22.2168 | 7.8125 | 341.13 KB |

### Pipeline Execution - Linear Chain
| Method       | Size | Mean        | Error     | StdDev    | Gen0   | Gen1   | Allocated |
|------------- |----- |------------:|----------:|----------:|-------:|-------:|----------:|
| **Execute**      | **10**   |    **573.6 ns** |   **0.56 ns** |   **0.50 ns** | **0.1612** | **0.0010** |   **2.47 KB** |
| ExecuteAsync | 10   |  5,407.6 ns |  58.91 ns |  55.10 ns | 0.3967 |      - |   6.05 KB |
| **Execute**      | **50**   |  **2,882.7 ns** |   **4.44 ns** |   **3.46 ns** | **0.6027** | **0.0153** |   **9.28 KB** |
| ExecuteAsync | 50   | 25,566.8 ns | 348.97 ns | 326.43 ns | 2.0752 | 0.0610 |  31.85 KB |
| **Execute**      | **100**  |  **7,966.8 ns** |  **11.64 ns** |   **9.72 ns** | **2.0752** | **0.1678** |  **31.96 KB** |
| ExecuteAsync | 100  | 47,391.8 ns | 535.85 ns | 501.23 ns | 3.6011 | 0.1831 |  54.73 KB |

### Pipeline Execution - Wide Fan-Out
| Method       | Size | Mean      | Error     | StdDev    | Gen0   | Gen1   | Allocated |
|------------- |----- |----------:|----------:|----------:|-------:|-------:|----------:|
| **Execute**      | **10**   |  **1.130 μs** | **0.0022 μs** | **0.0019 μs** | **0.3262** | **0.0057** |   **5.02 KB** |
| ExecuteAsync | 10   |  5.069 μs | 0.0838 μs | 0.0784 μs | 0.3967 |      - |   6.04 KB |
| **Execute**      | **50**   |  **3.739 μs** | **0.0028 μs** | **0.0024 μs** | **0.9460** | **0.0381** |  **14.51 KB** |
| ExecuteAsync | 50   | 18.943 μs | 0.1871 μs | 0.1563 μs | 1.8616 | 0.0916 |   28.4 KB |
| **Execute**      | **100**  |  **6.237 μs** | **0.0060 μs** | **0.0051 μs** | **1.3351** | **0.0687** |  **20.52 KB** |
| ExecuteAsync | 100  | 33.439 μs | 0.3500 μs | 0.3274 μs | 3.2349 | 0.2441 |  49.41 KB |

### Pipeline Execution - Complex Graph
| Method       | Size | Mean      | Error     | StdDev    | Gen0   | Gen1   | Allocated |
|------------- |----- |----------:|----------:|----------:|-------:|-------:|----------:|
| **Execute**      | **5**    |  **2.265 μs** | **0.0049 μs** | **0.0041 μs** | **0.4845** | **0.0114** |   **7.44 KB** |
| ExecuteAsync | 5    | 14.877 μs | 0.2929 μs | 0.3134 μs | 1.0376 | 0.0153 |  15.96 KB |
| **Execute**      | **10**   |  **6.779 μs** | **0.0564 μs** | **0.0527 μs** | **1.7853** | **0.1068** |  **27.34 KB** |
| ExecuteAsync | 10   | 29.679 μs | 0.5351 μs | 0.5005 μs | 2.1362 | 0.0916 |  32.51 KB |
| **Execute**      | **25**   | **12.017 μs** | **0.0558 μs** | **0.0466 μs** | **2.5024** | **0.2289** |  **38.55 KB** |
| ExecuteAsync | 25   | 71.522 μs | 0.7303 μs | 0.6474 μs | 5.9814 | 0.7324 |  91.73 KB |

### Pipeline Serialization
| Method              | Size | Mean       | Error     | StdDev    | Gen0    | Gen1    | Gen2    | Allocated |
|-------------------- |----- |-----------:|----------:|----------:|--------:|--------:|--------:|----------:|
| **Serialize_Linear**    | **5**    |   **7.311 μs** | **0.0566 μs** | **0.0529 μs** |  **1.8997** |  **0.0153** |       **-** |  **29.23 KB** |
| Serialize_Complex   | 5    |  10.647 μs | 0.0120 μs | 0.0112 μs |  2.8839 |  0.0458 |       - |  44.46 KB |
| Deserialize_Linear  | 5    |  27.673 μs | 0.0888 μs | 0.0830 μs |  5.3406 |  0.5493 |       - |  81.83 KB |
| Deserialize_Complex | 5    |  45.779 μs | 0.0543 μs | 0.0453 μs |  8.1177 |  1.0986 |       - |  125.2 KB |
| RoundTrip_Linear    | 5    |  35.749 μs | 0.0309 μs | 0.0274 μs |  7.2021 |  0.6714 |       - | 111.06 KB |
| RoundTrip_Complex   | 5    |  57.303 μs | 0.0429 μs | 0.0335 μs | 11.0474 |  1.8311 |       - | 169.67 KB |
| **Serialize_Linear**    | **10**   |  **14.505 μs** | **0.0549 μs** | **0.0459 μs** |  **3.7842** |  **0.1068** |       **-** |  **58.23 KB** |
| Serialize_Complex   | 10   |  22.199 μs | 0.0269 μs | 0.0224 μs |  5.7678 |  0.0916 |       - |  88.72 KB |
| Deserialize_Linear  | 10   |  55.284 μs | 0.2181 μs | 0.1934 μs | 10.9253 |  2.0142 |       - |  167.6 KB |
| Deserialize_Complex | 10   |  91.243 μs | 0.1643 μs | 0.1456 μs | 16.6016 |  4.1504 |       - | 254.33 KB |
| RoundTrip_Linear    | 10   |  71.477 μs | 0.1019 μs | 0.0903 μs | 14.6484 |  2.4414 |       - | 225.82 KB |
| RoundTrip_Complex   | 10   | 113.355 μs | 0.0740 μs | 0.0656 μs | 22.3389 |  7.3242 |       - | 343.05 KB |
| **Serialize_Linear**    | **25**   |  **59.383 μs** | **0.0305 μs** | **0.0270 μs** | **29.3579** | **29.3579** | **29.3579** | **145.29 KB** |
| Serialize_Complex   | 25   |  89.682 μs | 0.0621 μs | 0.0550 μs | 47.6074 | 47.6074 | 47.6074 | 221.55 KB |
| Deserialize_Linear  | 25   | 135.879 μs | 0.1238 μs | 0.0967 μs | 26.8555 |  9.5215 |       - | 412.29 KB |
| Deserialize_Complex | 25   | 226.605 μs | 0.8157 μs | 0.7630 μs | 41.0156 | 22.2168 |       - |  628.6 KB |
| RoundTrip_Linear    | 25   | 199.743 μs | 0.3436 μs | 0.2869 μs | 58.5938 | 29.2969 | 29.2969 | 557.58 KB |
| RoundTrip_Complex   | 25   | 356.275 μs | 0.9243 μs | 0.8645 μs | 47.3633 | 47.3633 | 47.3633 | 850.12 KB |

### Pipeline Build & Serialization
| Method                | NodeCount | Mean        | Error       | StdDev      | Gen0    | Gen1   | Gen2   | Allocated |
|---------------------- |---------- |------------:|------------:|------------:|--------:|-------:|-------:|----------:|
| **Build_Serial**          | **2**         |  **5,501.1 ns** |    **20.19 ns** |    **16.86 ns** |  **1.9684** | **0.1602** |      **-** |  **30.17 KB** |
| Build_FanOut          | 2         |  6,571.6 ns |    21.39 ns |    18.96 ns |  2.0752 | 0.1831 |      - |  31.85 KB |
| SerializeDefinition   | 2         |    819.6 ns |     1.48 ns |     1.31 ns |  0.1783 |      - |      - |   2.73 KB |
| SerializeInputs       | 2         |    385.6 ns |     1.00 ns |     0.93 ns |  0.0787 |      - |      - |   1.21 KB |
| DeserializeDefinition | 2         |  2,634.7 ns |     4.06 ns |     3.39 ns |  0.3777 |      - |      - |    5.8 KB |
| DeserializeInputs     | 2         |  2,543.1 ns |    17.80 ns |    14.87 ns |  0.5379 | 0.0114 |      - |   8.24 KB |
| LoadExecutor          | 2         |    678.5 ns |     6.51 ns |     5.77 ns |  0.0935 | 0.0038 | 0.0019 |   1.44 KB |
| **Build_Serial**          | **5**         |  **9,570.9 ns** |    **99.38 ns** |    **82.98 ns** |  **2.4567** | **0.2136** |      **-** |  **37.65 KB** |
| Build_FanOut          | 5         | 10,897.1 ns |   214.34 ns |   333.70 ns |  2.5330 | 0.2289 |      - |  38.93 KB |
| SerializeDefinition   | 5         |  1,941.0 ns |     9.88 ns |     7.72 ns |  0.4387 |      - |      - |   6.73 KB |
| SerializeInputs       | 5         |    427.0 ns |     7.48 ns |     6.63 ns |  0.0939 |      - |      - |   1.45 KB |
| DeserializeDefinition | 5         |  7,276.6 ns |   113.77 ns |   106.42 ns |  0.9689 | 0.0229 |      - |  14.91 KB |
| DeserializeInputs     | 5         |  2,728.5 ns |    52.56 ns |    73.69 ns |  0.5417 | 0.0076 |      - |   8.35 KB |
| LoadExecutor          | 5         |  1,684.0 ns |    32.57 ns |    36.20 ns |  0.2022 | 0.0038 | 0.0019 |   3.14 KB |
| **Build_Serial**          | **10**        | **16,434.5 ns** |   **211.79 ns** |   **176.86 ns** |  **3.3569** | **0.3662** |      **-** |  **51.77 KB** |
| Build_FanOut          | 10        | 17,817.6 ns |   312.34 ns |   347.17 ns |  3.3569 | 0.3357 |      - |  51.52 KB |
| SerializeDefinition   | 10        |  3,944.9 ns |    63.44 ns |    59.34 ns |  0.8698 | 0.0076 |      - |  13.38 KB |
| SerializeInputs       | 10        |    484.7 ns |     9.60 ns |    13.14 ns |  0.1192 |      - |      - |   1.84 KB |
| DeserializeDefinition | 10        | 14,556.9 ns |   223.83 ns |   209.37 ns |  1.9989 | 0.0916 |      - |  30.66 KB |
| DeserializeInputs     | 10        |  2,737.4 ns |    36.55 ns |    30.52 ns |  0.5493 | 0.0076 |      - |   8.46 KB |
| LoadExecutor          | 10        |  3,077.5 ns |    41.13 ns |    34.35 ns |  0.4044 | 0.0076 |      - |    6.2 KB |
| **Build_Serial**          | **20**        | **29,591.4 ns** |   **447.56 ns** |   **396.75 ns** |  **5.1880** | **0.6714** |      **-** |  **79.49 KB** |
| Build_FanOut          | 20        | 30,992.4 ns |   478.26 ns |   423.97 ns |  5.0049 | 0.6714 |      - |  76.84 KB |
| SerializeDefinition   | 20        |  7,531.8 ns |   147.56 ns |   186.61 ns |  1.7395 | 0.0229 |      - |  26.74 KB |
| SerializeInputs       | 20        |    615.4 ns |     2.25 ns |     1.99 ns |  0.1707 |      - |      - |   2.62 KB |
| DeserializeDefinition | 20        | 30,558.1 ns |   578.85 ns |   666.60 ns |  4.0283 | 0.3967 |      - |  62.05 KB |
| DeserializeInputs     | 20        |  3,043.7 ns |    60.16 ns |    82.34 ns |  0.5722 | 0.0114 |      - |   8.79 KB |
| LoadExecutor          | 20        |  5,901.0 ns |    60.53 ns |    56.62 ns |  0.8011 | 0.0153 |      - |  12.34 KB |
| **Build_Serial**          | **50**        | **67,479.9 ns** |   **251.01 ns** |   **209.60 ns** | **10.2539** | **2.1973** |      **-** | **157.16 KB** |
| Build_FanOut          | 50        | 70,966.9 ns | 1,172.28 ns | 1,039.19 ns |  9.5215 | 1.8311 |      - | 146.92 KB |
| SerializeDefinition   | 50        | 18,073.5 ns |   356.05 ns |   365.63 ns |  4.3335 | 0.0610 |      - |  66.81 KB |
| SerializeInputs       | 50        |  1,023.8 ns |    20.51 ns |    35.39 ns |  0.3223 |      - |      - |   4.96 KB |
| DeserializeDefinition | 50        | 75,550.7 ns |   940.06 ns |   879.33 ns | 10.0098 | 2.1973 |      - | 153.52 KB |
| DeserializeInputs     | 50        |  3,477.4 ns |    32.01 ns |    26.73 ns |  0.6371 | 0.0191 |      - |   9.77 KB |
| LoadExecutor          | 50        | 14,125.9 ns |   234.41 ns |   219.27 ns |  1.8921 | 0.0305 |      - |  29.09 KB |

### Pipeline Context
| Method                | PortCount | Mean         | Error      | StdDev     | Median       | Gen0   | Gen1   | Allocated |
|---------------------- |---------- |-------------:|-----------:|-----------:|-------------:|-------:|-------:|----------:|
| **Write_SingleInt**       | **1**         |    **528.85 ns** |   **9.364 ns** |   **7.820 ns** |    **529.50 ns** | **0.3481** | **0.0057** |    **5464 B** |
| Read_SingleInt        | 1         |     18.35 ns |   0.358 ns |   0.335 ns |     18.36 ns |      - |      - |         - |
| Read_SingleIntMissing | 1         |    346.87 ns |   6.923 ns |  14.298 ns |    344.47 ns | 0.2518 | 0.0024 |    3952 B |
| WriteRead_SingleInt   | 1         |    551.79 ns |   9.043 ns |   7.552 ns |    549.09 ns | 0.3500 | 0.0067 |    5496 B |
| Write_SingleString    | 1         |    408.18 ns |   6.464 ns |   6.916 ns |    405.11 ns | 0.2608 | 0.0038 |    4096 B |
| Write_ManyInts        | 1         |    515.66 ns |   9.619 ns |   8.033 ns |    514.32 ns | 0.3519 | 0.0067 |    5528 B |
| Read_ManyInts         | 1         |     16.06 ns |   0.024 ns |   0.021 ns |     16.06 ns |      - |      - |         - |
| Write_DirectGuid      | 1         |    525.48 ns |  10.188 ns |  12.128 ns |    523.02 ns | 0.3481 | 0.0057 |    5464 B |
| Read_DirectGuid       | 1         |     13.32 ns |   0.054 ns |   0.045 ns |     13.30 ns |      - |      - |         - |
| **Write_SingleInt**       | **10**        |    **512.83 ns** |  **10.133 ns** |   **8.461 ns** |    **508.70 ns** | **0.3481** | **0.0057** |    **5464 B** |
| Read_SingleInt        | 10        |     17.60 ns |   0.137 ns |   0.128 ns |     17.59 ns |      - |      - |         - |
| Read_SingleIntMissing | 10        |    332.22 ns |   4.841 ns |   3.780 ns |    332.43 ns | 0.2518 | 0.0024 |    3952 B |
| WriteRead_SingleInt   | 10        |    585.76 ns |  11.595 ns |  27.782 ns |    586.54 ns | 0.3500 | 0.0067 |    5496 B |
| Write_SingleString    | 10        |    425.80 ns |   7.971 ns |  12.410 ns |    427.74 ns | 0.2608 | 0.0038 |    4096 B |
| Write_ManyInts        | 10        |  2,186.98 ns |  43.590 ns |  88.054 ns |  2,169.39 ns | 0.7782 | 0.0267 |   12248 B |
| Read_ManyInts         | 10        |    167.04 ns |   1.097 ns |   0.972 ns |    166.66 ns |      - |      - |         - |
| Write_DirectGuid      | 10        |    519.56 ns |  10.269 ns |  21.661 ns |    516.50 ns | 0.3481 | 0.0057 |    5464 B |
| Read_DirectGuid       | 10        |     14.03 ns |   0.069 ns |   0.057 ns |     14.03 ns |      - |      - |         - |
| **Write_SingleInt**       | **100**       |    **529.48 ns** |  **10.432 ns** |  **16.547 ns** |    **530.38 ns** | **0.3481** | **0.0057** |    **5464 B** |
| Read_SingleInt        | 100       |     17.46 ns |   0.024 ns |   0.019 ns |     17.45 ns |      - |      - |         - |
| Read_SingleIntMissing | 100       |    330.32 ns |   5.914 ns |   7.895 ns |    327.37 ns | 0.2518 | 0.0024 |    3952 B |
| WriteRead_SingleInt   | 100       |    544.66 ns |   3.795 ns |   3.364 ns |    544.58 ns | 0.3500 | 0.0067 |    5496 B |
| Write_SingleString    | 100       |    421.78 ns |   8.339 ns |   8.190 ns |    423.09 ns | 0.2608 | 0.0038 |    4096 B |
| Write_ManyInts        | 100       | 14,549.77 ns | 278.962 ns | 310.066 ns | 14,489.77 ns | 4.4098 | 0.6256 |   69296 B |
| Read_ManyInts         | 100       |  1,973.70 ns |  31.757 ns |  28.151 ns |  1,962.59 ns |      - |      - |         - |
| Write_DirectGuid      | 100       |    519.65 ns |  10.261 ns |  19.769 ns |    511.20 ns | 0.3481 | 0.0057 |    5464 B |
| Read_DirectGuid       | 100       |     13.80 ns |   0.299 ns |   0.356 ns |     13.63 ns |      - |      - |         - |

### Pipeline Execution (Topology Variants)
| Method                 | NodeCount | Mean        | Error     | StdDev    | Median      | Gen0   | Gen1   | Allocated |
|----------------------- |---------- |------------:|----------:|----------:|------------:|-------:|-------:|----------:|
| **Execute_Serial**         | **2**         |    **983.7 ns** |  **19.61 ns** |  **33.83 ns** |    **965.1 ns** | **0.3872** | **0.0076** |   **5.95 KB** |
| Execute_FanOut         | 2         |  1,267.4 ns |  43.82 ns | 127.81 ns |  1,225.2 ns | 0.4082 | 0.0076 |   6.27 KB |
| Execute_FanOutToSerial | 2         |  2,023.6 ns |  39.33 ns |  46.82 ns |  2,020.5 ns | 0.4921 | 0.0076 |   7.59 KB |
| Execute_BinaryOut      | 2         |  1,138.5 ns |  22.52 ns |  58.14 ns |  1,113.9 ns | 0.4082 | 0.0076 |   6.27 KB |
| **Execute_Serial**         | **5**         |  **1,558.9 ns** |  **27.63 ns** |  **53.24 ns** |  **1,535.5 ns** | **0.4520** | **0.0095** |   **6.93 KB** |
| Execute_FanOut         | 5         |  1,722.5 ns |   8.48 ns |   7.51 ns |  1,720.7 ns | 0.4730 | 0.0095 |   7.26 KB |
| Execute_FanOutToSerial | 5         |  3,579.5 ns |  41.26 ns |  38.60 ns |  3,558.2 ns | 0.6866 | 0.0153 |  10.54 KB |
| Execute_BinaryOut      | 5         |  1,901.9 ns |  37.88 ns |  40.53 ns |  1,904.4 ns | 0.4940 | 0.0114 |   7.59 KB |
| **Execute_Serial**         | **10**        |  **2,401.9 ns** |  **27.39 ns** |  **21.38 ns** |  **2,403.4 ns** | **0.5569** | **0.0114** |   **8.57 KB** |
| Execute_FanOut         | 10        |  2,582.2 ns |   8.63 ns |   7.21 ns |  2,581.9 ns | 0.5798 | 0.0114 |    8.9 KB |
| Execute_FanOutToSerial | 10        |  9,606.9 ns | 159.12 ns | 141.05 ns |  9,591.3 ns | 1.9989 | 0.1373 |  30.63 KB |
| Execute_BinaryOut      | 10        |  4,288.1 ns |  17.42 ns |  13.60 ns |  4,288.9 ns | 1.0223 | 0.0381 |  15.73 KB |
| **Execute_Serial**         | **20**        |  **5,430.3 ns** |  **26.22 ns** |  **24.53 ns** |  **5,428.7 ns** | **1.1215** | **0.0381** |  **17.27 KB** |
| Execute_FanOut         | 20        |  5,549.7 ns |  23.21 ns |  20.57 ns |  5,544.3 ns | 1.1215 | 0.0458 |  17.19 KB |
| Execute_FanOutToSerial | 20        | 19,290.3 ns | 297.07 ns | 277.88 ns | 19,192.4 ns | 4.0283 | 0.4883 |  61.95 KB |
| Execute_BinaryOut      | 20        |  9,529.7 ns |  35.58 ns |  31.54 ns |  9,525.3 ns | 2.0294 | 0.1221 |  31.24 KB |
| **Execute_Serial**         | **50**        | **12,946.9 ns** |  **43.66 ns** |  **38.71 ns** | **12,940.4 ns** | **2.4414** | **0.1678** |  **37.48 KB** |
| Execute_FanOut         | 50        | 13,273.2 ns | 204.09 ns | 209.59 ns | 13,182.5 ns | 2.4872 | 0.1831 |  38.11 KB |
| Execute_FanOutToSerial | 50        | 47,463.8 ns | 615.54 ns | 575.77 ns | 47,414.0 ns | 9.4604 | 2.0752 | 145.72 KB |
| Execute_BinaryOut      | 50        | 15,834.2 ns | 102.44 ns |  95.82 ns | 15,828.9 ns | 2.7161 | 0.2136 |  41.95 KB |

*Note*: The `NodeCount` parameter reflects the initialization count, not the actual node count in the resulting graph. Different topologies (Serial, FanOut, FanOutToSerial, BinaryOut) produce different total node counts for the same `NodeCount` value.

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
