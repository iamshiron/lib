using Shiron.Lib.Collections.Bucket;
using Xunit;

namespace Shiron.Lib.Tests.Collections;

public class ConcurrentBucketTests {
    private ConcurrentBucket<string> CreateBucket() => new();

    [Fact]
    public void Set_And_Get_TypedValue() {
        var bucket = CreateBucket();
        bucket.Set("key1", 42);

        Assert.Equal(42, bucket.Get<int>("key1"));
    }

    [Fact]
    public void Set_And_Get_DifferentTypes() {
        var bucket = CreateBucket();
        bucket.Set("int", 42);
        bucket.Set("str", "hello");
        bucket.Set("double", 3.14);

        Assert.Equal(42, bucket.Get<int>("int"));
        Assert.Equal("hello", bucket.Get<string>("str"));
        Assert.Equal(3.14, bucket.Get<double>("double"));
    }

    [Fact]
    public void Get_MissingKey_ReturnsDefault() {
        var bucket = CreateBucket();

        Assert.Equal(0, bucket.Get<int>("missing"));
        Assert.Null(bucket.Get<string>("missing"));
    }

    [Fact]
    public void TryGet_ExistingKey_ReturnsTrueAndValue() {
        var bucket = CreateBucket();
        bucket.Set("key", 42);

        var result = bucket.TryGet("key", out int value);

        Assert.True(result);
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGet_MissingKey_ReturnsFalse() {
        var bucket = CreateBucket();

        Assert.False(bucket.TryGet("missing", out int value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void TryGet_WrongType_ReturnsFalse() {
        var bucket = CreateBucket();
        bucket.Set("key", 42);

        Assert.False(bucket.TryGet("key", out string? _));
    }

    [Fact]
    public void Set_SameKey_DifferentType_EvictsOldEntry() {
        var bucket = CreateBucket();
        bucket.Set("key", 42);
        bucket.Set("key", "hello");

        Assert.Equal("hello", bucket.Get<string>("key"));
        Assert.Equal(0, bucket.Get<int>("key"));
    }

    [Fact]
    public void GetAny_ReturnsBoxedValue() {
        var bucket = CreateBucket();
        bucket.Set("key", 42);

        Assert.Equal(42, bucket.GetAny("key"));
    }

    [Fact]
    public void GetAny_MissingKey_ReturnsNull() {
        var bucket = CreateBucket();

        Assert.Null(bucket.GetAny("missing"));
    }

    [Fact]
    public void TryGetAny_ExistingKey_ReturnsTrueAndBoxedValue() {
        var bucket = CreateBucket();
        bucket.Set("key", 42);

        var result = bucket.TryGetAny("key", out var value);

        Assert.True(result);
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGetAny_MissingKey_ReturnsFalse() {
        var bucket = CreateBucket();

        Assert.False(bucket.TryGetAny("missing", out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Remove_ExistingKey_ReturnsTrue() {
        var bucket = CreateBucket();
        bucket.Set("key", 42);

        Assert.True(bucket.Remove<int>("key"));
        Assert.Equal(0, bucket.Get<int>("key"));
    }

    [Fact]
    public void Remove_MissingKey_ReturnsFalse() {
        var bucket = CreateBucket();

        Assert.False(bucket.Remove<int>("missing"));
    }

    [Fact]
    public void Remove_WrongType_ReturnsFalse() {
        var bucket = CreateBucket();
        bucket.Set("key", 42);

        Assert.False(bucket.Remove<string>("key"));
    }

    [Fact]
    public void RemoveAny_ExistingKey_ReturnsTrue() {
        var bucket = CreateBucket();
        bucket.Set("key", 42);

        Assert.True(bucket.RemoveAny("key"));
        Assert.Null(bucket.GetAny("key"));
    }

    [Fact]
    public void RemoveAny_MissingKey_ReturnsFalse() {
        var bucket = CreateBucket();

        Assert.False(bucket.RemoveAny("missing"));
    }

    [Fact]
    public void RemoveAny_WorksWithAnyStoredType() {
        var bucket = CreateBucket();
        bucket.Set("key", 42);

        Assert.True(bucket.RemoveAny("key"));
        Assert.Equal(0, bucket.Get<int>("key"));
    }

    [Fact]
    public void Set_Overwrites_ExistingSameTypeValue() {
        var bucket = CreateBucket();
        bucket.Set("key", 1);
        bucket.Set("key", 2);

        Assert.Equal(2, bucket.Get<int>("key"));
    }

    [Fact]
    public void MultipleKeys_IndependentStorage() {
        var bucket = CreateBucket();
        bucket.Set("a", 1);
        bucket.Set("b", 2);
        bucket.Set("c", 3);

        Assert.Equal(1, bucket.Get<int>("a"));
        Assert.Equal(2, bucket.Get<int>("b"));
        Assert.Equal(3, bucket.Get<int>("c"));
    }

    [Fact]
    public void Set_SameKey_DifferentType_GetAnyReflectsLatestType() {
        var bucket = CreateBucket();
        bucket.Set("key", 42);
        bucket.Set("key", "hello");

        Assert.Equal("hello", bucket.GetAny("key"));
    }

    [Fact]
    public void RemoveAny_ThenSet_SameKeyWorks() {
        var bucket = CreateBucket();
        bucket.Set("key", 42);
        bucket.RemoveAny("key");
        bucket.Set("key", "hello");

        Assert.Equal("hello", bucket.Get<string>("key"));
    }
}
