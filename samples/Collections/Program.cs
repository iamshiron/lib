using Shiron.Lib.Collections;

var buffer = new RingBuffer(512);

var rng = new Random();
var bytes = GC.GetAllocatedBytesForCurrentThread();
for (var i = 0; i < 5000; ++i) buffer.Add(rng.Next(0, 1000));
Console.WriteLine($"Median: {buffer.GetMedian()}, Average: {buffer.GetAverage()}, Count: {buffer.Count}, Capacity: {buffer.Capacity}");
Console.WriteLine($"Allocated bytes: {GC.GetAllocatedBytesForCurrentThread() - bytes}");
