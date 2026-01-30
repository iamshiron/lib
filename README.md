# Shiron.Lib

[![CI](https://github.com/iamshiron/lib/actions/workflows/ci.yml/badge.svg)](https://github.com/iamshiron/lib/actions/workflows/ci.yml)
[![Code Quality](https://github.com/iamshiron/lib/actions/workflows/code-quality.yml/badge.svg)](https://github.com/iamshiron/lib/actions/workflows/code-quality.yml)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A collection of high-performance C# utility libraries targeting **.NET 10.0**. These modules provide the infrastructure for game engine architecture, offering zero-allocation logging, instrumentation profiling, and systems interaction helpers.

## Modules
### 1. Shiron.Lib.Logging
**Zero-Allocation Structured Logging** designed for high-frequency render loops. It decouples data capture from formatting to minimize GC pressure in hot paths.

* **Hot-Path Safety:** Uses `struct` payloads and pooled resources.
* **Dual-Mode Output:**
    * **Console:** Human-readable formatting for development.
    * **NDJSON:** Machine-readable structured output for observability (e.g., Seq, Datadog).
* **Context Tracking:** Maintains execution context across `async`/`await` boundaries using `AsyncLocal`.

**Example Output (NDJSON)**
*Capturing Vulkan hardware state without allocations: (added indentation for showcase)*
```json
{
    "timestamp": 1769749518056,
    "level": 0,
    "type": "...VulkanPhysicalDeviceLogEntry",
    "body": {
        "deviceName":"NVIDIA GeForce RTX 4080",
        "deviceType":"DiscreteGpu",
        "vendorId":4318,
        "deviceId":9988
    }
}
```

### 2. Shiron.Lib.Profiling
Instrumentation-based profiling library compatible with the **Chrome Trace Event Format** (`chrome://tracing`).
* **Scoped Measurement:** `ProfileScope` struct for `using` block instrumentation.
* **Visual Analysis:** serialized JSON output can be loaded into Perfetto or Chrome Tracing.

```csharp
using Shiron.Lib.Profiling;

// Initialize with optional real-time logging
var profiler = new Profiler(logger, logProfiling: true);

using (new ProfileScope(profiler, "PhysicsStep")) {
    // Execution time recorded automatically upon disposal
    SimulatePhysics();
}

// Flush events to disk (./profiles/)
profiler.SaveToFile("frame_capture");
```

### 3. Shiron.Lib.Utils
Static helpers for system operations and unsafe data manipulation.

* **System Interaction (`ShellUtils`):** Wrapper for `Process` to execute shell commands with captured `stdout`/`stderr` and environment control.
* **Data Integrity (`HashUtils`):** Simple to use SHA256 computation for files and directory trees.
* **Runtime (`FunctionUtils`):** Reflection helpers to convert `MethodInfo` into typed Delegates (falling back to Expression Trees for performance).
* **Platform (`PlatformUtils`):** OS and Architecture detection for cross-platform logic.

## Building
```bash
dotnet build --configuration Release
```

### Directory Structure
* `src/`: Source code for Logging, Profiling, and Utils.
* `samples/`: Reference implementations and usage patterns.
* `profiles/`: Default output directory for trace artifacts.

## Used By
* [ManilaBuild](https://github.com/iamshiron/ManilaBuild): Polyglot build system (WIP).

## 📄 License
This project is licensed under the [MIT License](LICENSE).
