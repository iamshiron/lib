namespace Shiron.Lib.Collections;

/// <summary>
/// Represents a mutable directed acyclic graph (DAG).
/// </summary>
/// <typeparam name="T">The type of the nodes in the graph.</typeparam>
public interface IDag<T> : IReadOnlyDag<T> where T : notnull {
    /// <summary>
    /// Adds a node to the graph. Does nothing if the node already exists.
    /// </summary>
    /// <param name="node">The node to add.</param>
    void AddNode(T node);

    /// <summary>
    /// Adds a directed edge from <paramref name="from"/> to <paramref name="to"/>.
    /// Both nodes are added to the graph if they don't already exist.
    /// </summary>
    /// <param name="from">The source node of the edge.</param>
    /// <param name="to">The target node of the edge.</param>
    /// <exception cref="InvalidOperationException">Thrown when adding the edge would create a cycle.</exception>
    void AddEdge(T from, T to);

    /// <summary>
    /// Removes a node and all its connected edges from the graph.
    /// </summary>
    /// <param name="node">The node to remove.</param>
    void RemoveNode(T node);

    /// <summary>
    /// Removes the directed edge between two nodes.
    /// </summary>
    /// <param name="from">The source node of the edge.</param>
    /// <param name="to">The target node of the edge.</param>
    void RemoveEdge(T from, T to);
}
