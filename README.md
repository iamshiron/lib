# Shiron.Lib

[![CI](https://github.com/iamshiron/lib/actions/workflows/ci.yml/badge.svg)](https://github.com/iamshiron/lib/actions/workflows/ci.yml)
[![Code Quality](https://github.com/iamshiron/lib/actions/workflows/code-quality.yml/badge.svg)](https://github.com/iamshiron/lib/actions/workflows/code-quality.yml)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)


A .NET 10 library providing collections, profiling, logging, networking, and utility functions.

## Components

### Collections

**RingBuffer** - Circular buffer for statistics tracking with O(1) average and standard deviation calculations.

```csharp
using Shiron.Lib.Collections;

var buffer = new RingBuffer(512); // power-of-two capacity required

// Track your metrics
for (var i = 0; i < 5000; i++) {
    buffer.Add(frameTimeMs);
}

// Get statistics in O(1) - no sorting needed
double avg = buffer.GetAverage();
double stdDev = buffer.GetStandardDeviation();

// When you need more detailed analysis
double median = buffer.GetMedian();
double low1Percent = buffer.GetAverageLowPercentile(0.01);
double high1Percent = buffer.GetAverageHighPercentile(0.01);
```

Maintains running sums for constant-time statistics. See [benchmarks](BENCHMARKS.md) for performance numbers.

### Profiling

Outputs Chrome Trace Event Format for viewing in `chrome://tracing`. Tracks execution times and memory allocations.

```csharp
using Shiron.Lib.Profiling;

var profiler = new Profiler(logger);

using (new ProfileCategory("Initialization")) {
    using (new ProfileScope(profiler, "Load Config")) {
        await LoadConfigurationAsync();
    }

    using (new ProfileScope(profiler, "Connect to Database")) {
        await ConnectToDatabaseAsync();
    }
}

// Nested scopes and parallel tasks are automatically tracked
using (new ProfileScope(profiler, "Parallel Processing")) {
    await Task.WhenAll(
        Task.Run(() => {
            using (new ProfileScope(profiler, "Worker A")) {
                ProcessDataA();
            }
        }),
        Task.Run(() => {
            using (new ProfileScope(profiler, "Worker B")) {
                ProcessDataB();
            }
        })
    );
}

// Save trace file for chrome://tracing
profiler.SaveToFile("profiles");
```

### Logging

Structured logging with log levels, hierarchical sub-loggers, custom renderers, and log injection for testing.

```csharp
using Shiron.Lib.Logging;

var logger = new Logger(jsonMode: false);
logger.AddRenderer(new ConsoleLogRenderer());

// Standard levels with colorized output
logger.Info("Application started");
logger.Warning("Cache miss - rebuilding index");
logger.Error("Failed to connect to service");

// Create hierarchical loggers
var dbLogger = logger.CreateSubLogger("Database");
var queryLogger = dbLogger.CreateSubLogger("Queries");

dbLogger.Info("Connection established");
queryLogger.Debug("SELECT * FROM users WHERE id = @id");

// Capture logs for testing without suppressing them
var injector = new LogInjector(logger, suppress: false);
using (injector.Inject()) {
    logger.Info("This will be captured AND displayed");
}

injector.Replay(testLogger);
```

### Networking

LiteNetLib wrapper with command-based message handling. Uses MemoryPack for serialization.

**Common:**
The library is strongly typed and thus needs a protocol that is used by both the client and the server.
You can usually define this in a classlib you reference in both your server and client.
```csharp
// Define your packets with MemoryPack
[MemoryPackable]
public partial class ChatMessage {
    public string Sender { get; set; }
    public string Message { get; set; }
}
```

**Server:**
```csharp
using Shiron.Lib.Networking;

var registry = new CommandRegistry();
registry.Register<PingCommand>(0x1000);
registry.Register<ChatMessage>(0x1001);

var server = new NetworkServer(registry);

server.OnClientConnected += (id) =>
    Console.WriteLine($"Client {id} joined");

server.OnPacketReceived += (clientId, packet) => {
    if (packet is ChatMessage msg) {
        Console.WriteLine($"[{msg.Sender}]: {msg.Message}");
        server.Broadcast(msg, DeliveryMethod.ReliableOrdered);
    }
};

server.Start(5566);

while (true) {
    server.Poll();
    Thread.Sleep(15);
}
```

**Client:**
```csharp
var client = new NetworkClient(registry);

client.OnConnected += () => Console.WriteLine("Connected!");
client.OnPacketReceived += (packet) => {
    if (packet is ChatMessage msg) {
        Console.WriteLine($"{msg.Sender}: {msg.Message}");
    }
};

client.Connect("127.0.0.1", 5566);

// Send messages
client.Send(new ChatMessage {
    Sender = username,
    Message = "Hello, world!"
}, DeliveryMethod.ReliableOrdered);
```

### Utilities

**HashUtils** - SHA256 hashing for files, strings, and objects:
```csharp
using Shiron.Lib.Utils;

// Hash individual files
string fileHash = HashUtils.HashFile("config.json");

// Hash a set of files for content-addressable caching
var projectFiles = Directory.GetFiles("src", "*.cs", SearchOption.AllDirectories);
string projectHash = HashUtils.CreateFileSetHash(projectFiles, root: "src");

// Hash arbitrary objects via JSON serialization
string objectHash = HashUtils.HashObject(configObject);
```

**ShellUtils** - Execute shell commands with proper logging and error handling:
```csharp
var result = ShellUtils.Run("dotnet", ["build", "--configuration", "Release"],
    workingDir: projectDir, logger: logger);

if (result.ExitCode != 0) {
    logger.Error($"Build failed: {result.StdErr}");
}

// Suppress output for sensitive operations
var authResult = ShellUtils.RunSuppressed("gh", ["auth", "login"]);
```

**TimeUtils** - Timestamp utilities for consistent time handling:
```csharp
long now = TimeUtils.Now(); // milliseconds since epoch
string formatted = TimeUtils.FormatTimestamp(now); // "14:32:15.432"
```

**PlatformUtils** - Cross-platform detection:
```csharp
string platform = PlatformUtils.GetPlatformKey(); // "windows", "linux", "osx"
string arch = PlatformUtils.GetArchitectureKey(); // "x64", "arm64"
```

**FunctionUtils** - Convert MethodInfo to delegates safely:
```csharp
var method = typeof(MyClass).GetMethod("Calculate");
var del = FunctionUtils.ToDelegate(instance, method);
```

## Performance

See [benchmarks](BENCHMARKS.md) for detailed performance data.

## Getting Started

Add Shiron.Lib as a git submodule to your project:

```bash
git submodule add <repository-url> lib/Shiron.Lib
git submodule update --init --recursive
```

Then reference the projects you need in your `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\lib\Shiron.Lib\src\Collections\Shiron.Lib.Collections.csproj" />
  <ProjectReference Include="..\lib\Shiron.Lib\src\Logging\Shiron.Lib.Logging.csproj" />
  <ProjectReference Include="..\lib\Shiron.Lib\src\Networking\Shiron.Lib.Networking.csproj" />
  <ProjectReference Include="..\lib\Shiron.Lib\src\Profiling\Shiron.Lib.Profiling.csproj" />
  <ProjectReference Include="..\lib\Shiron.Lib\src\Utils\Shiron.Lib.Utils.csproj" />
</ItemGroup>
```

All projects target .NET 10.

## Examples

Working examples in `samples/`:
- **Collections** - RingBuffer statistics
- **Profiling** - Chrome trace generation
- **Misc** - Logging and GC tracking
- **Networking** - Client/server chat

Run any sample:
```bash
dotnet run --project <sample_to_run> -c Release
```

## License

MIT
