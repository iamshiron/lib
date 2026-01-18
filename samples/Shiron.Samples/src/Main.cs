using System.Security.AccessControl;
using System.Security.Cryptography;
using Shiron.Logging;
using Shiron.Logging.Renderer;
using Shiron.Profiling;
using Shiron.Samples;

// Allocate some gc trackers so they don't appear during profiling
var gcTracker1 = new GCTracker();
var gcTracker2 = new GCTracker();
var gcTracker3 = new GCTracker();

gcTracker1.Start();
var jsonLogging = args.Contains("--json");
ILogRenderer renderer = new LogRenderer();

var logger = new Logger(jsonLogging);
if (!jsonLogging) {
    logger.AddRenderer(renderer);
}

var sub = logger.CreateSubLogger("sub");
var subSub = sub.CreateSubLogger("2");

var profiler = new Profiler(logger, true);

gcTracker2.Start();
using (new ProfileScope(profiler, "Sample Scope")) {
    logger.Info("This is an informational message.");
    logger.Warning("This is a warning message.");
    logger.Error("This is an error message.");
    logger.Debug("This is a debug message.");

    sub.Debug("This is a debug message from sub-logger.");
    sub.Info("This is an info message from sub-logger.");
    sub.Warning("This is a warning message from sub-logger.");
    sub.Error("This is an error message from sub-logger.");

    subSub.Debug("This is a debug message from sub-sub-logger.");
    subSub.Info("This is an info message from sub-sub-logger.");
    subSub.Warning("This is a warning message from sub-sub-logger.");
    subSub.Error("This is an error message from sub-sub-logger.");

    var outerInjector = new LogInjector(logger, (entry) => { });
    LogInjector injector = new LogInjector(logger, (entry) => { }, true);

    using (outerInjector.Inject()) {
        logger.Info("This info log will be captured by the outer injector which will not suppress the log.");
        using (injector.Inject()) {
            logger.Info("This info log will be captured by the inner injector which will suppress the log.");
        }
    }

    logger.Info($"Outer Logs Captured: {outerInjector.CapturedEntries.Count}, Inner Logs Captured: {injector.CapturedEntries.Count}");
    logger.Info("Outer Logs:");
    outerInjector.Replay(logger);
    logger.Info("Inner Logs:");
    injector.Replay(logger);
}

// Spam the logger to generate better GC results
gcTracker3.Start();
long maxLogs = 1000;
for (int i = 0; i < maxLogs; ++i) {
    var randInt = RandomNumberGenerator.GetInt32(0, 1000);
    if (randInt % 5 == 0) {
        logger.Debug($"Debug message");
    } else if (randInt % 5 == 1) {
        logger.Info($"Info message");
    } else if (randInt % 5 == 2) {
        logger.Warning($"Warning message");
    } else if (randInt % 5 == 3) {
        logger.Error($"Error message");
    } else {
        logger.System($"System message");
    }
}

long spamRes = gcTracker3.End();
long res = gcTracker2.End();


if (!Directory.Exists("profiles")) {
    _ = Directory.CreateDirectory("profiles");
}

profiler.SaveToFile("profiles");

Console.WriteLine($"Total bytes allocated: {gcTracker1.End()}");
Console.WriteLine($"Hot path bytes allocated: {res}");
Console.WriteLine($"Spam path bytes allocated: {spamRes}, over {maxLogs} logs. Avg: {spamRes / maxLogs} bytes/log");
