namespace Shiron.Lib.Collections;

/// <inheritdoc/>
public class DirectedAcyclicGraph<T> : IDag<T> where T : notnull {
    private readonly Dictionary<T, HashSet<T>> _adjacencyList = [];
    private readonly Dictionary<T, HashSet<T>> _reverseAdjacencyList = [];

    /// <inheritdoc/>
    public IEnumerable<T> Nodes => _adjacencyList.Keys;

    /// <inheritdoc/>
    public void AddNode(T node) {
        if (!_adjacencyList.ContainsKey(node)) {
            _adjacencyList[node] = [];
            _reverseAdjacencyList[node] = [];
        }
    }

    /// <inheritdoc/>
    public void AddEdge(T from, T to) {
        AddNode(from);
        AddNode(to);

        if (HasPath(to, from)) throw new InvalidOperationException($"Adding edge from '{from}' to '{to}' creates a cycle.");

        _adjacencyList[from].Add(to);
        _reverseAdjacencyList[to].Add(from);
    }

    /// <inheritdoc/>
    public void RemoveEdge(T from, T to) {
        if (_adjacencyList.TryGetValue(from, out var children)) {
            children.Remove(to);
        }

        if (_reverseAdjacencyList.TryGetValue(to, out var parents)) {
            parents.Remove(from);
        }
    }

    /// <inheritdoc/>
    public void RemoveNode(T node) {
        if (!_adjacencyList.ContainsKey(node)) return;
        foreach (var parent in _reverseAdjacencyList[node]) _adjacencyList[parent].Remove(node);
        foreach (var child in _adjacencyList[node]) _reverseAdjacencyList[child].Remove(node);

        _adjacencyList.Remove(node);
        _reverseAdjacencyList.Remove(node);
    }

    /// <inheritdoc/>
    public IEnumerable<T> GetChildren(T node) {
        return _adjacencyList.TryGetValue(node, out var children) ? children : Enumerable.Empty<T>();
    }

    /// <inheritdoc/>
    public IEnumerable<T> GetParents(T node) {
        return _reverseAdjacencyList.TryGetValue(node, out var parents) ? parents : Enumerable.Empty<T>();
    }

    private bool HasPath(T start, T target) {
        if (start.Equals(target)) return true;

        var visited = new HashSet<T>();
        var stack = new Stack<T>();
        stack.Push(start);

        while (stack.Count > 0) {
            var current = stack.Pop();
            if (!visited.Add(current)) continue;

            foreach (var neighbor in GetChildren(current)) {
                if (neighbor.Equals(target)) return true;
                stack.Push(neighbor);
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public IEnumerable<T> TopologicalSort() {
        var sorted = new List<T>();
        var inDegrees = new Dictionary<T, int>();
        var zeroInDegreeQueue = new Queue<T>();

        foreach (var node in Nodes) {
            var degree = GetParents(node).Count();
            inDegrees[node] = degree;
            if (degree == 0) {
                zeroInDegreeQueue.Enqueue(node);
            }
        }

        while (zeroInDegreeQueue.Count > 0) {
            var current = zeroInDegreeQueue.Dequeue();
            sorted.Add(current);

            foreach (var child in GetChildren(current)) {
                inDegrees[child]--;
                if (inDegrees[child] == 0) {
                    zeroInDegreeQueue.Enqueue(child);
                }
            }
        }

        if (sorted.Count != _adjacencyList.Count) throw new InvalidOperationException("Graph contains a cycle and cannot be topologically sorted.");
        return sorted;
    }

    /// <inheritdoc/>
    public T[][] ToLayers() {
        var layers = new List<T[]>();
        var inDegrees = new Dictionary<T, int>();
        var currentLayer = new List<T>();

        foreach (var node in Nodes) {
            var degree = GetParents(node).Count();
            inDegrees[node] = degree;
            if (degree == 0) currentLayer.Add(node);
        }

        while (currentLayer.Count > 0) {
            layers.Add(currentLayer.ToArray());
            var nextLayer = new List<T>();

            foreach (var node in currentLayer) {
                foreach (var child in GetChildren(node)) {
                    inDegrees[child]--;
                    if (inDegrees[child] == 0) nextLayer.Add(child);
                }
            }

            currentLayer = nextLayer;
        }

        if (layers.SelectMany(l => l).Count() != _adjacencyList.Count)
            throw new InvalidOperationException("Graph contains a cycle and cannot be layered.");

        return layers.ToArray();
    }
}
