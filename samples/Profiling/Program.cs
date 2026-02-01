
using Shiron.Lib.Profiling;

var profiler = new Profiler(null);

var startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
var startAllocations = GC.GetTotalAllocatedBytes(true);

using (new ProfileCategory("Init")) {
    using (new ProfileScope(profiler, "Load Config")) {
        Task.Delay(150).Wait();
    }
}

using (new ProfileCategory("Processing")) {
    using (new ProfileScope(profiler, "Step 1")) {
        Task.Delay(200).Wait();
    }

    using (new ProfileScope(profiler, "Step 2")) {
        Task.Delay(300).Wait();
    }

    using (new ProfileScope(profiler, "Step 3")) {
        Task.WaitAll(
            Task.Run(() => {
                using (new ProfileScope(profiler, "Subtask A")) {
                    Task.Delay(100).Wait();
                }
            }),
            Task.Run(() => {
                using (new ProfileScope(profiler, "Subtask B")) {
                    Task.Delay(150).Wait();
                }
            }),
            Task.Run(() => {
                using (new ProfileScope(profiler, "Subtask C")) {
                    Task.Delay(200).Wait();
                }
            }
        ));
    }
}

// Allocation intensive task
for (int x = 0; x < 1000; ++x) {
    using (new ProfileScope(profiler, "Allocation Task")) {
    }
}

using (new ProfileCategory("Finalizing")) {
    using (new ProfileScope(profiler, "Cleanup")) {
        Task.Delay(100).Wait();
    }
}

var endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
var endAllocations = GC.GetTotalAllocatedBytes(true);

System.Console.WriteLine($"Total Time: {endTime - startTime} ms");
System.Console.WriteLine($"Total Allocations: {endAllocations - startAllocations} bytes");
System.Console.WriteLine($"Allocations/event: {(endAllocations - startAllocations) / (double) profiler.TraceEvents.Length} bytes/event");

profiler.SaveToFile("profiles");
