
using Shiron.Lib.Flow;

var debouncer = new Throttler(1750);

while (true) {
    Thread.Sleep(50);
    if (debouncer.TryDebounce()) {
        Console.WriteLine($"Debounce at {DateTimeOffset.UtcNow}");
    }
}
