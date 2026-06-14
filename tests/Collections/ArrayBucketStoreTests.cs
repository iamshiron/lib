using System.Collections.Concurrent;
using Shiron.Lib.Collections.Bucket;
using Xunit;

namespace Shiron.Lib.Tests.Collections;

public class ArrayBucketStoreTests : BucketStoreContractTests<int> {
    private const int BucketSize = 16;

    private static ArrayBucketStore CreateArrayStore() {
        return new ArrayBucketStore(new Dictionary<Type, int> {
            [typeof(int)] = BucketSize,
            [typeof(double)] = BucketSize,
            [typeof(object)] = BucketSize,
        });
    }

    protected override IBucketStore<int> CreateStore() => CreateArrayStore();

    protected override int Key1 => 0;
    protected override int Key2 => 1;
    protected override int Key3 => 2;

    [Fact]
    public void Has_InBounds_ReturnsTrue() {
        var store = CreateArrayStore();
        Assert.True(store.Has<int>(5));
        Assert.True(store.Has<int>(0));
        Assert.True(store.Has<int>(BucketSize - 1));
    }

    [Fact]
    public void Has_OutOfBounds_ReturnsFalse() {
        var store = CreateArrayStore();
        Assert.False(store.Has<int>(BucketSize));
        Assert.False(store.Has<int>(BucketSize + 10));
    }

    [Fact]
    public void Has_NegativeKey_ReturnsFalse() {
        var store = CreateArrayStore();
        Assert.False(store.Has<int>(-1));
    }

    [Fact]
    public void Has_ReferenceType_InBounds_ReturnsTrue() {
        var store = CreateArrayStore();
        Assert.True(store.Has<string>(5));
    }

    [Fact]
    public void HasAny_AlwaysReturnsTrue() {
        var store = CreateArrayStore();
        Assert.True(store.HasAny(0));
        Assert.True(store.HasAny(BucketSize));
        Assert.True(store.HasAny(-1));
    }

    [Fact]
    public void TypeOf_NonexistentKey_ReturnsNull() {
        var store = CreateArrayStore();
        Assert.Null(store.TypeOf(99));
    }

    [Fact]
    public void GetAs_RegisteredType_ReturnsValue() {
        var store = CreateArrayStore();
        store.Set(0, 42);
        Assert.Equal(42, store.GetAs<int>(0));
    }

    [Fact]
    public void Get_UnregisteredValueType_Throws() {
        var store = CreateArrayStore();
        Assert.Throws<InvalidOperationException>(() => store.Get<float>(0));
    }

    [Fact]
    public void Get_ReferenceTypeOnValueTypeKey_ReturnsDefault() {
        var store = CreateArrayStore();
        store.Set(0, 42);
        Assert.Null(store.Get<string>(0));
    }

    [Fact]
    public void GetAs_UnregisteredType_Throws() {
        var store = CreateArrayStore();
        Assert.Throws<InvalidOperationException>(() => store.GetAs<float>(0));
    }

    [Fact]
    public void GetAny_NonexistentKey_ReturnsNull() {
        var store = CreateArrayStore();
        Assert.Null(store.GetAny(99));
    }

    [Fact]
    public void TryGetAny_NonexistentKey_ReturnsFalse() {
        var store = CreateArrayStore();
        var result = store.TryGetAny(99, out var value);
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void CanCast_NonexistentKey_ReturnsFalse() {
        var store = CreateArrayStore();
        Assert.False(store.CanCast<int>(99));
    }

    [Fact]
    public void Has_ReferenceType_OutOfBounds_ReturnsFalse() {
        var store = CreateArrayStore();
        Assert.False(store.Has<string>(BucketSize));
    }

    [Fact]
    public void Has_UnregisteredValueType_Throws() {
        var store = CreateArrayStore();
        Assert.Throws<InvalidOperationException>(() => store.Has<float>(0));
    }

    [Fact]
    public void Set_NonGenericOverload_StoresValueType() {
        var store = CreateArrayStore();
        store.Set(0, 42, typeof(int));
        Assert.Equal(42, store.Get<int>(0));
        Assert.Equal(typeof(int), store.TypeOf(0));
    }

    [Fact]
    public void Set_NonGenericOverload_StoresReferenceType() {
        var store = CreateArrayStore();
        store.Set(1, "hello", typeof(string));
        Assert.Equal("hello", store.Get<string>(1));
        Assert.Equal(typeof(string), store.TypeOf(1));
    }

    [Fact]
    public void TryGet_ReferenceType_ReturnsTrueAndValue() {
        var store = CreateArrayStore();
        store.Set(0, "hello");
        var result = store.TryGet(0, out string? value);
        Assert.True(result);
        Assert.Equal("hello", value);
    }

    [Fact]
    public void TryGet_ReferenceType_Nonexistent_ReturnsFalse() {
        var store = CreateArrayStore();
        var result = store.TryGet(0, out string? value);
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void Set_SameKeyDifferentValueType_EvictsOldTypedData() {
        var store = CreateArrayStore();
        store.Set(0, 42);
        store.Set(0, 3.14);

        Assert.Equal(typeof(double), store.TypeOf(0));
        Assert.Equal(3.14, store.Get<double>(0));
        Assert.Equal(3.14, store.GetAny(0));

        Assert.Equal(0, store.Get<int>(0));
    }

    [Fact]
    public void Set_SameKeyFromValueTypeToReferenceType_EvictsOldTypedData() {
        var store = CreateArrayStore();
        store.Set(0, 42);
        store.Set(0, "hello");

        Assert.Equal("hello", store.Get<string>(0));
        Assert.Equal("hello", store.GetAny(0));
        Assert.Equal(0, store.Get<int>(0));
    }

    [Fact]
    public void Set_SameKeyFromReferenceTypeToValueType_EvictsOldReferenceData() {
        var store = CreateArrayStore();
        store.Set(0, "hello");
        store.Set(0, 42);

        Assert.Equal(42, store.Get<int>(0));
        Assert.Equal(42, store.GetAny(0));
        Assert.Null(store.Get<string>(0));
    }

    [Fact]
    public void Remove_ThrowsNotImplemented() {
        var store = CreateArrayStore();
        store.Set(0, 42);
        Assert.Throws<NotImplementedException>(() => store.Remove<int>(0));
    }

    [Fact]
    public void RemoveAny_ThrowsNotImplemented() {
        var store = CreateArrayStore();
        store.Set(0, 42);
        Assert.Throws<NotImplementedException>(() => store.RemoveAny(0));
    }

    [Fact]
    public void Constructor_WithoutObjectBucket_DefaultsToEmptyReferenceBucket() {
        var store = new ArrayBucketStore(new Dictionary<Type, int> {
            [typeof(int)] = BucketSize,
        });

        store.Set(0, 42);
        Assert.Equal(42, store.Get<int>(0));
        Assert.False(store.Has<string>(0));
        Assert.Throws<IndexOutOfRangeException>(() => store.Set(0, "hello"));
    }

    [Fact]
    public void ConcurrentWrites_AllValuesConsistent() {
        var store = CreateArrayStore();

        Parallel.For(0, BucketSize, i => store.Set(i, i));

        for (var i = 0; i < BucketSize; i++) {
            Assert.Equal(i, store.Get<int>(i));
        }
    }

    [Fact]
    public void ConcurrentMixedOperations_DoesNotThrow() {
        var store = CreateArrayStore();
        var exceptions = new ConcurrentQueue<Exception>();

        Parallel.For(0, 500, i => {
            try {
                var key = i % BucketSize;
                store.Set(key, i);
                store.Get<int>(key);
                store.Has<int>(key);
                store.HasAny(key);
                store.GetAny(key);
                store.TryGet<int>(key, out _);
                store.TryGetAny(key, out _);
                store.TypeOf(key);
                store.CanCast<int>(key);
            } catch (Exception ex) {
                exceptions.Enqueue(ex);
            }
        });

        Assert.Empty(exceptions);
    }
}
