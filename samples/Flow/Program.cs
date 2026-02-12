using System.Diagnostics;
using Shiron.Lib.Flow;

var throttler = new LatchedThrottler(5000);
var debouncer = new LeadingDebouncer(100);

var shouldExit = false;
var burstUntil = 0L;

var thread = new Thread(() => {
    while (true) {
        var c = Console.ReadLine();
        if (c == "exit") {
            shouldExit = true;
            break;
        }
        if (c == "run") {
            throttler.Trigger();
        } else if (c == "reset") {
            throttler.Reset();
        } else if (c == "burst") {
            burstUntil = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 5;
        }
    }
});
thread.IsBackground = true;
thread.Start();

while (!shouldExit) {
    if (burstUntil > Stopwatch.GetTimestamp()) {
        Thread.Sleep(5);
    } else {
        Thread.Sleep(200);
    }

    if (throttler.Update()) {
        Console.WriteLine($"Update!");
    }
    if (debouncer.TryExecute()) {
        Console.WriteLine("Debounced!");
    }
}
