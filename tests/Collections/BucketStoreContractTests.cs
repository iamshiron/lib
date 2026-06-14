using Shiron.Lib.Collections.Bucket;
using Xunit;

namespace Shiron.Lib.Tests.Collections;

/// <summary>
/// Contract tests shared by every <see cref="IBucketStore{TK}"/> implementation.
/// </summary>
/// <remarks>
/// Only behaviour that is identical across implementations lives here. Each subclass
/// supplies the concrete store via <see cref="CreateStore"/> and a set of sample keys,
/// then adds its own implementation-specific tests.
/// </remarks>
/// <typeparam name="TK">The key type.</typeparam>
public abstract class BucketStoreContractTests<TK> where TK : IEquatable<TK> {
    protected abstract IBucketStore<TK> CreateStore();

    protected abstract TK Key1 { get; }
    protected abstract TK Key2 { get; }
    protected abstract TK Key3 { get; }

    [Fact]
    public void Set_Get_ReturnsStoredValue() {
        var store = CreateStore();
        store.Set(Key1, 42);
        Assert.Equal(42, store.Get<int>(Key1));
    }

    [Fact]
    public void ReferenceType_SetGet_ReturnsStoredValue() {
        var store = CreateStore();
        store.Set(Key1, "hello");
        Assert.Equal("hello", store.Get<string>(Key1));
    }

    [Fact]
    public void Set_SameKeySameType_OverwritesValue() {
        var store = CreateStore();
        store.Set(Key1, 42);
        store.Set(Key1, 99);
        Assert.Equal(99, store.Get<int>(Key1));
    }

    [Fact]
    public void ReferenceType_Overwrite_SameKeySameType() {
        var store = CreateStore();
        store.Set(Key1, "hello");
        store.Set(Key1, "world");
        Assert.Equal("world", store.Get<string>(Key1));
    }

    [Fact]
    public void MultipleTypes_StoredIndependently() {
        var store = CreateStore();
        store.Set(Key1, 42);
        store.Set(Key2, "hello");
        store.Set(Key3, 3.14);

        Assert.Equal(42, store.Get<int>(Key1));
        Assert.Equal("hello", store.Get<string>(Key2));
        Assert.Equal(3.14, store.Get<double>(Key3));
    }

    [Fact]
    public void TryGet_ExistingKey_ReturnsTrueAndValue() {
        var store = CreateStore();
        store.Set(Key1, 42);
        var result = store.TryGet(Key1, out int value);
        Assert.True(result);
        Assert.Equal(42, value);
    }

    [Fact]
    public void GetAny_ExistingKey_ReturnsValue() {
        var store = CreateStore();
        store.Set(Key1, 42);
        Assert.Equal(42, store.GetAny(Key1));
    }

    [Fact]
    public void GetAny_ReferenceType_ReturnsValue() {
        var store = CreateStore();
        store.Set(Key1, "hello");
        Assert.Equal("hello", store.GetAny(Key1));
    }

    [Fact]
    public void TryGetAny_ExistingKey_ReturnsTrueAndValue() {
        var store = CreateStore();
        store.Set(Key1, 42);
        var result = store.TryGetAny(Key1, out var value);
        Assert.True(result);
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGetAny_ReferenceType_ReturnsTrueAndValue() {
        var store = CreateStore();
        store.Set(Key1, "hello");
        var result = store.TryGetAny(Key1, out var value);
        Assert.True(result);
        Assert.Equal("hello", value);
    }

    [Fact]
    public void TypeOf_ReturnsCorrectType() {
        var store = CreateStore();
        store.Set(Key1, 42);
        Assert.Equal(typeof(int), store.TypeOf(Key1));
    }

    [Fact]
    public void ReferenceType_TypeOf_ReturnsCorrectType() {
        var store = CreateStore();
        store.Set(Key1, "hello");
        Assert.Equal(typeof(string), store.TypeOf(Key1));
    }

    [Fact]
    public void CanCast_RegisteredType_ReturnsTrue() {
        var store = CreateStore();
        store.Set(Key1, 42);
        Assert.True(store.CanCast<int>(Key1));
        Assert.True(store.CanCast<object>(Key1));
    }

    [Fact]
    public void CanCast_NonAssignableType_ReturnsFalse() {
        var store = CreateStore();
        store.Set(Key1, 42);
        Assert.False(store.CanCast<string>(Key1));
    }
}
