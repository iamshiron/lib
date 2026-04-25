using Shiron.Lib.Collections;

var buffer = new RingBuffer(512);

var rng = new Random();
var bytes = GC.GetAllocatedBytesForCurrentThread();
for (var i = 0; i < 5000; ++i) buffer.Add(rng.Next(0, 1000));
Console.WriteLine($"Median: {buffer.GetMedian()}, Average: {buffer.GetAverage()}, Count: {buffer.Count}, Capacity: {buffer.Capacity}");
Console.WriteLine($"Allocated bytes: {GC.GetAllocatedBytesForCurrentThread() - bytes}");

var graph = new DirectedAcyclicGraph<string>();

Console.WriteLine("--- 1. Building the DAG ---");
graph.AddNode("Wake Up");
graph.AddEdge("Wake Up", "Shower");
graph.AddEdge("Shower", "Get Dressed");
graph.AddEdge("Wake Up", "Make Coffee");
graph.AddEdge("Make Coffee", "Drink Coffee");
graph.AddEdge("Get Dressed", "Leave House");
graph.AddEdge("Drink Coffee", "Leave House");

Console.WriteLine("Graph built successfully!");

Console.WriteLine("\n--- 2. Testing Topological Sort ---");
var sorted = graph.TopologicalSort();
Console.WriteLine("Execution Order: " + string.Join(" -> ", sorted));

Console.WriteLine("\n--- 3. Testing Cycle Detection ---");
try {
    Console.WriteLine("Attempting to add edge: 'Leave House' -> 'Wake Up'...");
    graph.AddEdge("Leave House", "Wake Up");
    Console.WriteLine("FAIL: This line should not be reached.");
} catch (InvalidOperationException ex) {
    Console.WriteLine($"SUCCESS: Cycle prevented! Exception: {ex.Message}");
}

Console.WriteLine("\n--- 4. Testing Removals ---");
Console.WriteLine("Removing edge: 'Wake Up' -> 'Shower'...");
graph.RemoveEdge("Wake Up", "Shower");

Console.WriteLine("Removing node: 'Make Coffee'...");
graph.RemoveNode("Make Coffee");

Console.WriteLine("\n--- 5. Topological Sort After Removals ---");
var updatedSorted = graph.TopologicalSort();
Console.WriteLine("New Execution Order: " + string.Join(" -> ", updatedSorted));

var layerGraph = new DirectedAcyclicGraph<string>();
layerGraph.AddEdge("Wake Up", "Shower");
layerGraph.AddEdge("Wake Up", "Make Coffee");
layerGraph.AddEdge("Shower", "Get Dressed");
layerGraph.AddEdge("Make Coffee", "Drink Coffee");
layerGraph.AddEdge("Get Dressed", "Leave House");
layerGraph.AddEdge("Drink Coffee", "Leave House");

Console.WriteLine("\n--- 6. Testing ToLayers ---");
var layers = layerGraph.ToLayers();
for (var i = 0; i < layers.Length; i++) {
    Console.WriteLine($"Layer {i}: [{string.Join(", ", layers[i])}]");
}
