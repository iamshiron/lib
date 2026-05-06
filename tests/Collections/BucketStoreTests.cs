using System.Collections.Concurrent;
using Shiron.Lib.Collections.Bucket;
using Xunit;

namespace Shiron.Lib.Tests.Collections;

public class BucketStoreTests {
    protected virtual IBucketStore<string> CreateStore() {
        return new BucketStore<string>();
    }

    [Fact]
    public void Set_Get_ReturnsStoredValue() {
        var store = CreateStore();
        store.Set("key1", 42);
        Assert.Equal(42, store.Get<int>("key1"));
    }

    [Fact]
    public void Get_NonexistentKey_ReturnsDefault() {
        var store = CreateStore();
        Assert.Equal(0, store.Get<int>("nonexistent"));
    }

    [Fact]
    public void Get_WrongType_ReturnsDefault() {
        var store = CreateStore();
        store.Set("key1", 42);
        Assert.Null(store.Get<string>("key1"));
    }

    [Fact]
    public void TryGet_ExistingKey_ReturnsTrueAndValue() {
        var store = CreateStore();
        store.Set("key1", 42);
        var result = store.TryGet("key1", out int value);
        Assert.True(result);
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGet_NonexistentKey_ReturnsFalse() {
        var store = CreateStore();
        var result = store.TryGet("nonexistent", out int value);
        Assert.False(result);
        Assert.Equal(0, value);
    }

    [Fact]
    public void Set_SameKeySameType_OverwritesValue() {
        var store = CreateStore();
        store.Set("key1", 42);
        store.Set("key1", 99);
        Assert.Equal(99, store.Get<int>("key1"));
    }

    [Fact]
    public void Set_SameKeyDifferentType_EvictsOldEntry() {
        var store = CreateStore();
        store.Set("key1", 42);
        store.Set("key1", "hello");

        Assert.Equal(0, store.Get<int>("key1"));
        Assert.Equal("hello", store.Get<string>("key1"));
    }

    [Fact]
    public void GetAny_ExistingKey_ReturnsValue() {
        var store = CreateStore();
        store.Set("key1", 42);
        Assert.Equal(42, store.GetAny("key1"));
    }

    [Fact]
    public void GetAny_NonexistentKey_ReturnsNull() {
        var store = CreateStore();
        Assert.Null(store.GetAny("nonexistent"));
    }

    [Fact]
    public void TryGetAny_ExistingKey_ReturnsTrueAndValue() {
        var store = CreateStore();
        store.Set("key1", 42);
        var result = store.TryGetAny("key1", out var value);
        Assert.True(result);
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGetAny_NonexistentKey_ReturnsFalse() {
        var store = CreateStore();
        var result = store.TryGetAny("nonexistent", out var value);
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void Remove_ExistingKeyWithCorrectType_ReturnsTrue() {
        var store = CreateStore();
        store.Set("key1", 42);
        Assert.True(store.Remove<int>("key1"));
        Assert.Equal(0, store.Get<int>("key1"));
        Assert.False(store.HasAny("key1"));
    }

    [Fact]
    public void Remove_WrongType_ReturnsFalse() {
        var store = CreateStore();
        store.Set("key1", 42);
        Assert.False(store.Remove<string>("key1"));
        Assert.True(store.HasAny("key1"));
    }

    [Fact]
    public void Remove_NonexistentKey_ReturnsFalse() {
        var store = CreateStore();
        Assert.False(store.Remove<int>("nonexistent"));
    }

    [Fact]
    public void RemoveAny_ExistingKey_ReturnsTrue() {
        var store = CreateStore();
        store.Set("key1", 42);
        Assert.True(store.RemoveAny("key1"));
        Assert.False(store.HasAny("key1"));
    }

    [Fact]
    public void RemoveAny_NonexistentKey_ReturnsFalse() {
        var store = CreateStore();
        Assert.False(store.RemoveAny("nonexistent"));
    }

    [Fact]
    public void Has_ExistingKeyWithCorrectType_ReturnsTrue() {
        var store = CreateStore();
        store.Set("key1", 42);
        Assert.True(store.Has<int>("key1"));
    }

    [Fact]
    public void Has_WrongType_ReturnsTrue() {
        var store = CreateStore();
        store.Set("key1", 42);
        Assert.True(store.Has<string>("key1"));
    }

    [Fact]
    public void Has_NonexistentKey_ReturnsFalse() {
        var store = CreateStore();
        Assert.False(store.Has<int>("nonexistent"));
    }

    [Fact]
    public void HasAny_ExistingKey_ReturnsTrue() {
        var store = CreateStore();
        store.Set("key1", 42);
        Assert.True(store.HasAny("key1"));
    }

    [Fact]
    public void HasAny_NonexistentKey_ReturnsFalse() {
        var store = CreateStore();
        Assert.False(store.HasAny("nonexistent"));
    }

    [Fact]
    public void MultipleTypes_StoredIndependently() {
        var store = CreateStore();
        store.Set("intKey", 42);
        store.Set("stringKey", "hello");
        store.Set("doubleKey", 3.14);

        Assert.Equal(42, store.Get<int>("intKey"));
        Assert.Equal("hello", store.Get<string>("stringKey"));
        Assert.Equal(3.14, store.Get<double>("doubleKey"));
    }
}

public class ConcurrentBucketStoreTests : BucketStoreTests {
    protected override IBucketStore<string> CreateStore() {
        return new ConcurrentBucketStore<string>();
    }

    [Fact]
    public void ConcurrentWrites_AllValuesConsistent() {
        var store = CreateStore();
        const int count = 1000;

        Parallel.For(0, count, i => {
            store.Set($"key{i}", i);
        });

        for (var i = 0; i < count; i++) {
            Assert.Equal(i, store.Get<int>($"key{i}"));
        }
    }

    [Fact]
    public void ConcurrentReadsAndWrites_DoesNotThrow() {
        var store = CreateStore();
        const int count = 500;
        var exceptions = new ConcurrentQueue<Exception>();

        for (var i = 0; i < 100; i++) {
            store.Set($"key{i}", i);
        }

        Parallel.For(0, count, i => {
            try {
                var key = $"key{i % 100}";
                store.Set(key, i);
                store.Get<int>(key);
                store.Has<int>(key);
                store.HasAny(key);
                store.GetAny(key);
                store.TryGet<int>(key, out _);
                store.TryGetAny(key, out _);
            } catch (Exception ex) {
                exceptions.Enqueue(ex);
            }
        });

        Assert.Empty(exceptions);
    }

    [Fact]
    public void ConcurrentRemoves_EachKeyRemovedExactlyOnce() {
        var store = CreateStore();
        const int count = 1000;

        for (var i = 0; i < count; i++) {
            store.Set($"key{i}", i);
        }

        var removed = new ConcurrentBag<string>();
        Parallel.For(0, count, i => {
            if (store.Remove<int>($"key{i}")) {
                removed.Add($"key{i}");
            }
        });

        Assert.Equal(count, removed.Count);
        Assert.Equal(count, removed.Distinct().Count());
    }

    [Fact]
    public void ConcurrentMixedOperations_DoesNotThrow() {
        var store = CreateStore();
        const int count = 500;
        var exceptions = new ConcurrentQueue<Exception>();

        Parallel.For(0, count, i => {
            try {
                var key = $"key{i % 50}";
                store.Set(key, i);
                store.Get<int>(key);
                store.Has<int>(key);
                store.HasAny(key);
                store.GetAny(key);
                store.TryGet<int>(key, out _);
                store.TryGetAny(key, out _);
                if (i % 5 == 0) store.Remove<int>(key);
                if (i % 10 == 0) store.RemoveAny(key);
            } catch (Exception ex) {
                exceptions.Enqueue(ex);
            }
        });

        Assert.Empty(exceptions);
    }

    [Fact]
    public void ConcurrentSetSameKeyDifferentTypes_NoOrphanedEntries() {
        var store = CreateStore();
        const int count = 500;
        var exceptions = new ConcurrentQueue<Exception>();

        Parallel.For(0, count, i => {
            try {
                if (i % 2 == 0) {
                    store.Set("shared", i);
                } else {
                    store.Set("shared", $"str_{i}");
                }
            } catch (Exception ex) {
                exceptions.Enqueue(ex);
            }
        });

        Assert.Empty(exceptions);
        Assert.True(store.HasAny("shared"));
    }
}
