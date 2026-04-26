namespace Shiron.Lib.Collections;

/// <summary>
/// Represents a read-only directed acyclic graph (DAG).
/// </summary>
/// <typeparam name="T">The type of the nodes in the graph.</typeparam>
public interface IReadOnlyDag<T> where T : notnull {
    /// <summary>
    /// Gets the collection of all nodes in the graph.
    /// </summary>
    internal IEnumerable<T> Nodes { get; }

    /// <summary>
    /// Gets the children (outgoing neighbors) of the specified node.
    /// </summary>
    /// <param name="node">The node whose children to retrieve.</param>
    /// <returns>An enumerable of child nodes.</returns>
    internal IEnumerable<T> GetChildren(T node);

    /// <summary>
    /// Gets the parents (incoming neighbors) of the specified node.
    /// </summary>
    /// <param name="node">The node whose parents to retrieve.</param>
    /// <returns>An enumerable of parent nodes.</returns>
    internal IEnumerable<T> GetParents(T node);

    /// <summary>
    /// Returns all nodes in topological order, where each node appears before its children.
    /// </summary>
    /// <returns>An enumerable of nodes in topological order.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the graph contains a cycle.</exception>
    internal IEnumerable<T> TopologicalSort();

    /// <summary>
    /// Returns the nodes organized into layers based on their dependency depth.
    /// Nodes with no parents are in the first layer, nodes whose parents are all in previous layers follow.
    /// </summary>
    /// <returns>A jagged array where each element is a layer of nodes.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the graph contains a cycle.</exception>
    internal T[][] ToLayers();
}
