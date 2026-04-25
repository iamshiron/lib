using Shiron.Lib.Collections;
using Xunit;

namespace Shiron.Lib.Tests.Collections;

public class DirectedAcyclicGraphTests {
    private DirectedAcyclicGraph<string> CreateDag() => new();

    [Fact]
    public void Nodes_Reflects_AddedNodes() {
        var dag = CreateDag();
        dag.AddNode("a");
        dag.AddNode("b");
        dag.AddNode("a");

        Assert.Equal(2, dag.Nodes.Count());
        Assert.Contains("a", dag.Nodes);
        Assert.Contains("b", dag.Nodes);
    }

    [Fact]
    public void AddNode_Idempotent_DoesNotDuplicate() {
        var dag = CreateDag();
        dag.AddNode("x");
        dag.AddNode("x");
        dag.AddNode("x");

        Assert.Single(dag.Nodes);
    }

    [Fact]
    public void AddEdge_AutoAddsNodes() {
        var dag = CreateDag();
        dag.AddEdge("a", "b");

        Assert.Contains("a", dag.Nodes);
        Assert.Contains("b", dag.Nodes);
    }

    [Fact]
    public void AddEdge_EstablishesParentChild() {
        var dag = CreateDag();
        dag.AddEdge("a", "b");

        Assert.Contains("b", dag.GetChildren("a"));
        Assert.Contains("a", dag.GetParents("b"));
    }

    [Fact]
    public void AddEdge_ThrowsOnDirectCycle() {
        var dag = CreateDag();
        dag.AddEdge("a", "b");

        Assert.Throws<InvalidOperationException>(() => dag.AddEdge("b", "a"));
    }

    [Fact]
    public void AddEdge_ThrowsOnTransitiveCycle() {
        var dag = CreateDag();
        dag.AddEdge("a", "b");
        dag.AddEdge("b", "c");

        Assert.Throws<InvalidOperationException>(() => dag.AddEdge("c", "a"));
    }

    [Fact]
    public void AddEdge_SelfLoop_Throws() {
        var dag = CreateDag();

        Assert.Throws<InvalidOperationException>(() => dag.AddEdge("a", "a"));
    }

    [Fact]
    public void AddEdge_DuplicateEdge_IsIdempotent() {
        var dag = CreateDag();
        dag.AddEdge("a", "b");
        dag.AddEdge("a", "b");

        Assert.Single(dag.GetChildren("a"));
        Assert.Single(dag.GetParents("b"));
    }

    [Fact]
    public void RemoveEdge_BreaksConnection() {
        var dag = CreateDag();
        dag.AddEdge("a", "b");
        dag.RemoveEdge("a", "b");

        Assert.Empty(dag.GetChildren("a"));
        Assert.Empty(dag.GetParents("b"));
        Assert.Contains("a", dag.Nodes);
        Assert.Contains("b", dag.Nodes);
    }

    [Fact]
    public void RemoveEdge_NonexistentEdge_DoesNotThrow() {
        var dag = CreateDag();
        dag.AddNode("a");
        dag.AddNode("b");

        dag.RemoveEdge("a", "b");
        Assert.Empty(dag.GetChildren("a"));
    }

    [Fact]
    public void RemoveEdge_NonexistentNodes_DoesNotThrow() {
        var dag = CreateDag();

        dag.RemoveEdge("x", "y");
    }

    [Fact]
    public void RemoveNode_RemovesNodeAndAllReferences() {
        var dag = CreateDag();
        dag.AddEdge("a", "b");
        dag.AddEdge("b", "c");
        dag.AddEdge("d", "b");

        dag.RemoveNode("b");

        Assert.DoesNotContain("b", dag.Nodes);
        Assert.Empty(dag.GetChildren("a"));
        Assert.Empty(dag.GetParents("c"));
        Assert.Empty(dag.GetChildren("d"));
    }

    [Fact]
    public void RemoveNode_NonexistentNode_DoesNotThrow() {
        var dag = CreateDag();
        dag.RemoveNode("nonexistent");
    }

    [Fact]
    public void RemoveNode_AllowsPreviouslyBlockedEdges() {
        var dag = CreateDag();
        dag.AddEdge("a", "b");
        dag.AddEdge("b", "c");

        Assert.Throws<InvalidOperationException>(() => dag.AddEdge("c", "a"));

        dag.RemoveNode("b");
        dag.AddEdge("c", "a");

        Assert.Contains("a", dag.GetChildren("c"));
    }

    [Fact]
    public void GetChildren_ReturnsEmptyForNonexistentNode() {
        var dag = CreateDag();
        Assert.Empty(dag.GetChildren("nonexistent"));
    }

    [Fact]
    public void GetParents_ReturnsEmptyForNonexistentNode() {
        var dag = CreateDag();
        Assert.Empty(dag.GetParents("nonexistent"));
    }

    [Fact]
    public void GetChildren_ReturnsMultipleChildren() {
        var dag = CreateDag();
        dag.AddEdge("a", "b");
        dag.AddEdge("a", "c");
        dag.AddEdge("a", "d");

        var children = dag.GetChildren("a").ToList();
        Assert.Equal(3, children.Count);
        Assert.Contains("b", children);
        Assert.Contains("c", children);
        Assert.Contains("d", children);
    }

    [Fact]
    public void GetParents_ReturnsMultipleParents() {
        var dag = CreateDag();
        dag.AddEdge("a", "d");
        dag.AddEdge("b", "d");
        dag.AddEdge("c", "d");

        var parents = dag.GetParents("d").ToList();
        Assert.Equal(3, parents.Count);
        Assert.Contains("a", parents);
        Assert.Contains("b", parents);
        Assert.Contains("c", parents);
    }

    [Fact]
    public void TopologicalSort_LinearGraph() {
        var dag = CreateDag();
        dag.AddEdge("a", "b");
        dag.AddEdge("b", "c");
        dag.AddEdge("c", "d");

        var sorted = dag.TopologicalSort().ToList();

        Assert.Equal(4, sorted.Count);
        Assert.True(sorted.IndexOf("a") < sorted.IndexOf("b"));
        Assert.True(sorted.IndexOf("b") < sorted.IndexOf("c"));
        Assert.True(sorted.IndexOf("c") < sorted.IndexOf("d"));
    }

    [Fact]
    public void TopologicalSort_DiamondDependency() {
        var dag = CreateDag();
        //   a
        //  / \
        // b   c
        //  \ /
        //   d
        dag.AddEdge("a", "b");
        dag.AddEdge("a", "c");
        dag.AddEdge("b", "d");
        dag.AddEdge("c", "d");

        var sorted = dag.TopologicalSort().ToList();

        Assert.Equal(4, sorted.Count);
        Assert.True(sorted.IndexOf("a") < sorted.IndexOf("b"));
        Assert.True(sorted.IndexOf("a") < sorted.IndexOf("c"));
        Assert.True(sorted.IndexOf("b") < sorted.IndexOf("d"));
        Assert.True(sorted.IndexOf("c") < sorted.IndexOf("d"));
    }

    [Fact]
    public void TopologicalSort_IsolatedNodes() {
        var dag = CreateDag();
        dag.AddNode("a");
        dag.AddEdge("b", "c");

        var sorted = dag.TopologicalSort().ToList();

        Assert.Equal(3, sorted.Count);
        Assert.True(sorted.IndexOf("b") < sorted.IndexOf("c"));
        Assert.Contains("a", sorted);
    }

    [Fact]
    public void TopologicalSort_SingleNode() {
        var dag = CreateDag();
        dag.AddNode("a");

        var sorted = dag.TopologicalSort().ToList();
        Assert.Single(sorted);
        Assert.Contains("a", sorted);
    }

    [Fact]
    public void TopologicalSort_EmptyGraph() {
        var dag = CreateDag();

        var sorted = dag.TopologicalSort().ToList();
        Assert.Empty(sorted);
    }

    [Fact]
    public void ToLayers_LinearGraph() {
        var dag = CreateDag();
        dag.AddEdge("a", "b");
        dag.AddEdge("b", "c");

        var layers = dag.ToLayers();

        Assert.Equal(3, layers.Length);
        Assert.Equal(["a"], layers[0]);
        Assert.Equal(["b"], layers[1]);
        Assert.Equal(["c"], layers[2]);
    }

    [Fact]
    public void ToLayers_DiamondDependency() {
        var dag = CreateDag();
        dag.AddEdge("a", "b");
        dag.AddEdge("a", "c");
        dag.AddEdge("b", "d");
        dag.AddEdge("c", "d");

        var layers = dag.ToLayers();

        Assert.Equal(3, layers.Length);
        Assert.Equal(["a"], layers[0]);

        var layer1 = layers[1].ToHashSet();
        Assert.Equal(2, layer1.Count);
        Assert.Contains("b", layer1);
        Assert.Contains("c", layer1);

        Assert.Equal(["d"], layers[2]);
    }

    [Fact]
    public void ToLayers_IsolatedNodes_InFirstLayer() {
        var dag = CreateDag();
        dag.AddNode("a");
        dag.AddEdge("b", "c");

        var layers = dag.ToLayers();

        Assert.Equal(2, layers.Length);

        var layer0 = layers[0].ToHashSet();
        Assert.Equal(2, layer0.Count);
        Assert.Contains("a", layer0);
        Assert.Contains("b", layer0);

        Assert.Equal(["c"], layers[1]);
    }

    [Fact]
    public void ToLayers_EmptyGraph() {
        var dag = CreateDag();

        var layers = dag.ToLayers();
        Assert.Empty(layers);
    }

    [Fact]
    public void ToLayers_WideGraph() {
        var dag = CreateDag();
        dag.AddEdge("root", "a");
        dag.AddEdge("root", "b");
        dag.AddEdge("root", "c");
        dag.AddEdge("root", "d");

        var layers = dag.ToLayers();

        Assert.Equal(2, layers.Length);
        Assert.Equal(["root"], layers[0]);

        var layer1 = layers[1].ToHashSet();
        Assert.Equal(4, layer1.Count);
    }

    [Fact]
    public void RemoveEdge_AllowsReaddingPreviouslyCyclicEdge() {
        var dag = CreateDag();
        dag.AddEdge("a", "b");
        dag.AddEdge("b", "c");

        Assert.Throws<InvalidOperationException>(() => dag.AddEdge("c", "a"));

        dag.RemoveEdge("b", "c");
        dag.AddEdge("c", "a");

        Assert.Contains("a", dag.GetChildren("c"));
        Assert.Empty(dag.GetChildren("b"));
    }

    [Fact]
    public void ComplexGraph_IntegrationTest() {
        var dag = CreateDag();
        //     a   e
        //    / \   \
        //   b   c   f
        //    \ /   /
        //     d   /
        //      \ /
        //       g
        dag.AddEdge("a", "b");
        dag.AddEdge("a", "c");
        dag.AddEdge("b", "d");
        dag.AddEdge("c", "d");
        dag.AddEdge("d", "g");
        dag.AddEdge("e", "f");
        dag.AddEdge("f", "g");

        var layers = dag.ToLayers();

        Assert.Equal(4, layers.Length);
        Assert.Equal(2, layers[0].Length);
        Assert.Equal(3, layers[1].Length);
        Assert.Single(layers[2]);
        Assert.Single(layers[3]);

        var sorted = dag.TopologicalSort().ToList();
        Assert.Equal(7, sorted.Count);
        Assert.True(sorted.IndexOf("a") < sorted.IndexOf("b"));
        Assert.True(sorted.IndexOf("a") < sorted.IndexOf("c"));
        Assert.True(sorted.IndexOf("b") < sorted.IndexOf("d"));
        Assert.True(sorted.IndexOf("c") < sorted.IndexOf("d"));
        Assert.True(sorted.IndexOf("d") < sorted.IndexOf("g"));
        Assert.True(sorted.IndexOf("e") < sorted.IndexOf("f"));
        Assert.True(sorted.IndexOf("f") < sorted.IndexOf("g"));

        dag.RemoveNode("d");

        Assert.Throws<InvalidOperationException>(() => dag.AddEdge("c", "a"));

        var sortedAfterRemoval = dag.TopologicalSort().ToList();
        Assert.Equal(6, sortedAfterRemoval.Count);
        Assert.DoesNotContain("d", sortedAfterRemoval);
        Assert.True(sortedAfterRemoval.IndexOf("e") < sortedAfterRemoval.IndexOf("f"));
        Assert.True(sortedAfterRemoval.IndexOf("f") < sortedAfterRemoval.IndexOf("g"));
    }

    [Fact]
    public void AddEdge_ThenRemoveNode_ThenAddEdge_CycleDetectionWorks() {
        var dag = CreateDag();
        dag.AddEdge("a", "b");
        dag.AddEdge("c", "d");

        Assert.Throws<InvalidOperationException>(() => dag.AddEdge("b", "a"));

        dag.RemoveNode("b");
        dag.AddEdge("d", "a");

        var sorted = dag.TopologicalSort().ToList();
        Assert.Equal(3, sorted.Count);
        Assert.True(sorted.IndexOf("c") < sorted.IndexOf("d"));
        Assert.True(sorted.IndexOf("d") < sorted.IndexOf("a"));
    }

    [Fact]
    public void ToLayers_MultiLevelGraph() {
        var dag = CreateDag();
        // a -> b -> c -> d -> e
        dag.AddEdge("a", "b");
        dag.AddEdge("b", "c");
        dag.AddEdge("c", "d");
        dag.AddEdge("d", "e");

        var layers = dag.ToLayers();

        Assert.Equal(5, layers.Length);
        for (var i = 0; i < 5; i++) {
            Assert.Single(layers[i]);
        }
    }
}
