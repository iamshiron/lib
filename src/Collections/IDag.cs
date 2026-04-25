namespace Shiron.Lib.Collections;

public interface IDag<T> : IReadOnlyDag<T> where T : notnull {
    void AddNode(T node);
    void AddEdge(T from, T to);
    void RemoveNode(T node);
    void RemoveEdge(T from, T to);
}
