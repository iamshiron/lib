using System.Security.AccessControl;
using System.Security.Cryptography;
using Shiron.Logging;
using Shiron.Profiling;
using Shiron.Samples.Logging;

var logger = new Logger(null);
logger.AddRenderer(new LogRenderer());

var profiler = new Profiler(logger, true);

using (new ProfileScope(profiler, "Sample Scope")) {
    logger.Info("This is an informational message.");
    logger.Warning("This is a warning message.");
    logger.Error("This is an error message.");
    logger.Debug("This is a debug message.");
}

if (!Directory.Exists("profiles")) {
    _ = Directory.CreateDirectory("profiles");
}

profiler.SaveToFile("profiles");
