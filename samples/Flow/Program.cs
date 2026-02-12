using Shiron.Lib.Flow;

var throttler = new LatchedThrottler(5000);
var shouldExit = false;

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
        }
    }
});
thread.IsBackground = true;
thread.Start();

while (!shouldExit) {
    Thread.Sleep(50);
    if (throttler.Update()) {
        Console.WriteLine($"Update!");
    }
}
