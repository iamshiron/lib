# Shiron Libraries
[![CI](https://github.com/iamshiron/lib/actions/workflows/ci.yml/badge.svg)](https://github.com/iamshiron/lib/actions/workflows/ci.yml)
[![Code Quality](https://github.com/iamshiron/lib/actions/workflows/code-quality.yml/badge.svg)](https://github.com/iamshiron/lib/actions/workflows/code-quality.yml)

This repository contains a set of C\# utility libraries targeting **.NET 10.0**. It provides infrastructure for structured logging, performance profiling, and system interaction helpers used in my projects.

## Modules
### 1\. Shiron.Logging
A library for structured logging implementing the `ILogger` interface.
  * **Context Tracking:** Uses `AsyncLocal<Stack<Guid>>` in `LogContext` to maintain execution context across asynchronous calls.
  * **Log Injection:** Defines a `LogInjector` class to subscribe to log events. Injectors receive `ILogEntry` objects and can filter or process them (e.g., writing to console or file).
  * **Data Structure:** Log entries are defined as `ILogEntry` implementations (e.g., `BasicLogEntry`, `MarkupLogEntry`), containing timestamps, log levels, and context IDs.

### 2\. Shiron.Profiling
A profiling library that generates data compatible with the **Chrome Trace Event Format**.
  * **Trace Events:** Records events (Begin, End, Complete) with timestamps, process/thread IDs, and arguments.
  * **Scoping:** Provides a `ProfileScope` struct (implementing `IDisposable`) to measure the duration of code blocks automatically.
  * **Output:** Serializes collected events to JSON files, which can be loaded into `chrome://tracing` or similar analysis tools.

**Usage Example:**

```csharp
using Shiron.Profiling;

// Initialize profiler (optional: enable logging of profile events)
var profiler = new Profiler(logger, logProfiling: true);

using (new ProfileScope(profiler, "DataProcessing")) {
    // Code block execution time is recorded
    Thread.Sleep(100);
}

// Write collected events to disk, will not be saved if folder does not exist
profiler.SaveToFile("profiles");
```

### 3\. Shiron.Utils
A library containing static utility classes for system operations and data manipulation.
  * **ShellUtils:** A wrapper for `System.Diagnostics.Process` that executes shell commands. It captures `stdout` and `stderr` streams, sets environment variables (e.g., `TERM`), and provides synchronous execution methods.
  * **HashUtils:** Provides methods to compute SHA256 hashes for individual files or combined hashes for sets of files/directories.
  * **FunctionUtils:** Contains reflection helpers to convert `MethodInfo` into typed Delegates, falling back to Expression trees when `Delegate.CreateDelegate` is not applicable.
  * **RegexUtils:** Hosts pre-compiled `Regex` patterns for parsing specific string formats (e.g., job identifiers, plugin specifications).
  * **PlatformUtils & TimeUtils:** Helpers for detecting the operating system/architecture and formatting Unix timestamps.

## Building
The solution targets **.NET 9.0**. Build using the .NET CLI:

```bash
dotnet build --configuration Release
```

### Structure
  * `src/`: Source code for `Shiron.Logging`, `Shiron.Profiling`, and `Shiron.Utils`.
  * `samples/`: Sample project (`Shiron.Samples`) demonstrating library usage.
  * `profiles/`: Default output directory for profiling data.

## Projects using those libraries
- [ManilaBuild](https://github.com/iamshiran/ManilaBuild): A work in progress polyglot build system

## 📄 License
To be determined. All rights reserved by the author.
