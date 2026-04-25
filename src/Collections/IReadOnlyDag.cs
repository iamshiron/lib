namespace Shiron.Lib.Collections;

public interface IReadOnlyDag<T> where T : notnull {
    internal IEnumerable<T> Nodes { get; }
    internal IEnumerable<T> GetChildren(T node);
    internal IEnumerable<T> GetParents(T node);
    internal IEnumerable<T> TopologicalSort();
    internal T[][] ToLayers();
}
