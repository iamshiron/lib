using Shiron.Logging;
using Shiron.Samples.Logging;

var logger = new Logger(null);
_ = new LogRenderer(logger);

logger.Info("This is an informational message.");
logger.Warning("This is a warning message.");
logger.Error("This is an error message.");
logger.Debug("This is a debug message.");
