# Shiron.Lib

[![CI](https://github.com/iamshiron/lib/actions/workflows/ci.yml/badge.svg)](https://github.com/iamshiron/lib/actions/workflows/ci.yml)
[![Code Quality](https://github.com/iamshiron/lib/actions/workflows/code-quality.yml/badge.svg)](https://github.com/iamshiron/lib/actions/workflows/code-quality.yml)
[![License](https://img.shields.io/badge/license-AGPL--3.0-blue.svg)](LICENSE)


A .NET 10 library providing collections, flow control, profiling, logging, networking, Docker Compose parsing, and utility functions.

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

Structured logging with log levels, hierarchical sub-loggers, custom renderers, and log injection for testing. Supports contextual logging to track operation flow.

```csharp
using Shiron.Lib.Logging;

var logger = new Logger(jsonLogger: false);
logger.AddRenderer(new ConsoleLogRenderer());

// Standard levels with colorized output
logger.Info("Application started");
logger.Warning("Cache miss - rebuilding index");
logger.Error("Failed to connect to service");

// Contextual logging for tracking operation flow
logger.Info("Starting batch process", out var batchLogger);
batchLogger.Info("Processing item 1", out var itemLogger);
itemLogger.Debug("Validating schema...");
itemLogger.Info("Item 1 processed successfully");

batchLogger.Info("Processing item 2"); // Shares parent context with item 1

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

### Flow

Flow control utilities for throttling and debouncing operations.

**Throttler** - Rate-limit operations to a specified interval:
```csharp
using Shiron.Lib.Flow;

var throttler = new Throttler(500); // 500ms interval

while (true) {
    if (userInput && throttler.TryExecute()) {
        ProcessInput(); // Only executes once per 500ms
    }

    // Check cooldown progress
    float progress = throttler.CooldownProgress(); // 0.0 to 1.0
    float remaining = throttler.GetTimeRemainingMS();
}
```

**LatchedThrottler** - Throttler with latch mechanism for deferred execution:
```csharp
var latchedThrottler = new LatchedThrottler(1000);

// Signal that execution is needed
latchedThrottler.Trigger();

// In your update loop
if (latchedThrottler.Update()) {
    // Executes once per interval when triggered
    SaveConfiguration();
}
```

**LeadingDebouncer** - Execute immediately, then ignore subsequent calls for a silence period:
```csharp
var debouncer = new LeadingDebouncer(300);

// First call executes immediately, subsequent calls within 300ms are ignored
if (debouncer.TryExecute()) {
    SearchDatabase(query); // Executes on first input
}
```

**TrailingDebouncer** - Execute only after activity stops for a silence period:
```csharp
var debouncer = new TrailingDebouncer(500);

// Signal activity
debouncer.Signal();

// In your update loop
if (debouncer.TryResolve()) {
    // Executes 500ms after last Signal() call
    AutoSave();
}
```

### DockerUtils

> **Work in progress.** This module is tailored for the smaller [ComposeToNginx](https://github.com/iamshiron/ComposeToNginx) project — a CLI that reads a `docker-compose` file and pushes selected hosts to an NGINX Proxy Manager instance. The parsed shape may evolve as those needs do.

Reads Docker Compose files and extracts service definitions into uniform DTOs. Uses YamlDotNet to parse `services`, `image`, `container_name`, `restart`, `ports`, `volumes`, `environment`, and `networks`, including both short and long-form port definitions.

```csharp
using Shiron.Lib.DockerUtils;
using Shiron.Lib.DockerUtils.Model;

var reader = new ComposeReader();
IReadOnlyList<Service> services = reader.Read(File.ReadAllText("docker-compose.yml"));

foreach (var service in services) {
    Console.WriteLine($"{service.Name} -> {service.Image}");
    Console.WriteLine($"  Restart: {service.Restart}");
    foreach (var port in service.Ports) {
        Console.WriteLine($"  {port.HostPort}:{port.ContainerPort}");
    }
}
```

Malformed YAML or unsupported field shapes throw `ComposeReadException`.

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

**UUID** - RFC 4122 compliant UUIDv4 implementation:
```csharp
using Shiron.Lib.Utils;

// Generate a random UUIDv4
UUID id = UUID.Random();
Console.WriteLine(id.ToString()); // "f47ac10b-58cc-4372-a567-0e02b2c3d479"

// Parse from string
UUID parsed = UUID.FromString("550e8400-e29b-41d4-a716-446655440000");

// Efficiently format to Span
Span<char> buffer = stackalloc char[36];
if (id.TryFormat(buffer, out int written, default, null)) {
    // Use buffer...
}
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
  <ProjectReference Include="..\lib\Shiron.Lib\src\Flow\Shiron.Lib.Flow.csproj" />
  <ProjectReference Include="..\lib\Shiron.Lib\src\Logging\Shiron.Lib.Logging.csproj" />
  <ProjectReference Include="..\lib\Shiron.Lib\src\Networking\Shiron.Lib.Networking.csproj" />
  <ProjectReference Include="..\lib\Shiron.Lib\src\Profiling\Shiron.Lib.Profiling.csproj" />
  <ProjectReference Include="..\lib\Shiron.Lib\src\DockerUtils\Shiron.Lib.DockerUtils.csproj" />
  <ProjectReference Include="..\lib\Shiron.Lib\src\Utils\Shiron.Lib.Utils.csproj" />
</ItemGroup>
```

All projects target .NET 10.

## Examples

Working examples in `samples/`:
- **Collections** - RingBuffer statistics
- **Flow** - Interactive flow control visualization
- **Profiling** - Chrome trace generation
- **Misc** - Logging and GC tracking
- **Networking** - Client/server chat

Run any sample:
```bash
dotnet run --project <sample_to_run> -c Release
```

## License

GNU Affero General Public License v3.0 (AGPL-3.0)
