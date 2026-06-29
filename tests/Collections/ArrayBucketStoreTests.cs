using System.Collections.Concurrent;
using System.Text;
using Shiron.Lib.Collections.Bucket;
using Xunit;

namespace Shiron.Lib.Tests.Collections;

public class ArrayBucketStoreTests {
    private const int BucketSize = 16;

    private static ArrayBucketStore CreateStore() {
        return new ArrayBucketStore(new Dictionary<Type, int> {
            [typeof(int)] = BucketSize,
            [typeof(double)] = BucketSize,
            [typeof(object)] = BucketSize,
        });
    }

    public class Constructor {
        [Fact]
        public void NegativeSize_Throws() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArrayBucketStore(
                new Dictionary<Type, int> { [typeof(int)] = -1 }));
        }

        [Fact]
        public void EmptySizes_ProducesZeroCapacityStore() {
            var store = new ArrayBucketStore([]);
            Assert.False(store.HasAny(0));
            Assert.Null(store.GetAny(0));
            Assert.Throws<InvalidOperationException>(() => store.Set(0, 1));
        }

        [Fact]
        public void ReferenceBucket_SizedToMaxReferenceEntry() {
            var store = new ArrayBucketStore(new Dictionary<Type, int> {
                [typeof(int)] = 4,
                [typeof(object)] = 8,
                [typeof(string)] = 32,
            });

            store.Set(7, "edge");
            store.Set(31, "max");
            Assert.Equal("max", store.Get<string>(31));
            Assert.Throws<ArgumentOutOfRangeException>(() => store.Set(32, "out"));
        }
    }

    public class SetGet {
        [Fact]
        public void ValueType_RoundTrip() {
            var store = CreateStore();
            store.Set(0, 42);
            Assert.Equal(42, store.Get<int>(0));
        }

        [Fact]
        public void ValueType_Overwrite_SameType() {
            var store = CreateStore();
            store.Set(0, 42);
            store.Set(0, 99);
            Assert.Equal(99, store.Get<int>(0));
        }

        [Fact]
        public void MultipleKeys_StoredIndependently() {
            var store = CreateStore();
            store.Set(0, 1);
            store.Set(1, 2);
            store.Set(2, 3);
            Assert.Equal(1, store.Get<int>(0));
            Assert.Equal(2, store.Get<int>(1));
            Assert.Equal(3, store.Get<int>(2));
        }

        [Fact]
        public void MultipleTypes_StoredIndependently() {
            var store = CreateStore();
            store.Set(0, 42);
            store.Set(1, 3.14);
            Assert.Equal(42, store.Get<int>(0));
            Assert.Equal(3.14, store.Get<double>(1));
        }

        [Fact]
        public void ValueType_OutOfRange_Throws() {
            var store = CreateStore();
            Assert.Throws<ArgumentOutOfRangeException>(() => store.Set(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => store.Set(BucketSize, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => store.Get<int>(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => store.Get<int>(BucketSize));
        }

        [Fact]
        public void ValueType_UnregisteredType_GetThrows() {
            var store = CreateStore();
            Assert.Throws<InvalidOperationException>(() => store.Get<float>(0));
        }

        [Fact]
        public void ValueType_UnregisteredType_SetThrows() {
            var store = CreateStore();
            Assert.Throws<InvalidOperationException>(() => store.Set(0, 1f));
        }

        [Fact]
        public void ReferenceType_RoundTrip() {
            var store = CreateStore();
            store.Set(0, "hello");
            Assert.Equal("hello", store.Get<string>(0));
        }

        [Fact]
        public void ReferenceType_Overwrite_SameType() {
            var store = CreateStore();
            store.Set(0, "hello");
            store.Set(0, "world");
            Assert.Equal("world", store.Get<string>(0));
        }

        [Fact]
        public void ReferenceType_GetIncompatibleType_ReturnsDefault() {
            var store = CreateStore();
            store.Set(0, "hello");
            Assert.Null(store.Get<StringBuilder>(0));
        }

        [Fact]
        public void ReferenceType_WithoutObjectBucket_SetThrows() {
            var store = new ArrayBucketStore(new Dictionary<Type, int> {
                [typeof(int)] = BucketSize,
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => store.Set(0, "hello"));
        }
    }

    public class SetNonGeneric {
        [Fact]
        public void StoresValueType() {
            var store = CreateStore();
            store.Set(0, 42, typeof(int));
            Assert.Equal(42, store.Get<int>(0));
            Assert.Equal(typeof(int), store.TypeOf(0));
        }

        [Fact]
        public void StoresReferenceType() {
            var store = CreateStore();
            store.Set(1, "hello", typeof(string));
            Assert.Equal("hello", store.Get<string>(1));
            Assert.Equal(typeof(string), store.TypeOf(1));
        }
    }

    public class ReTyping {
        [Fact]
        public void ValueToValue_EvictsOldData() {
            var store = CreateStore();
            store.Set(0, 42);
            store.Set(0, 3.14);

            Assert.Equal(typeof(double), store.TypeOf(0));
            Assert.Equal(3.14, store.Get<double>(0));
            Assert.Equal(3.14, store.GetAny(0));
            Assert.Equal(0, store.Get<int>(0));
            Assert.False(store.Has<int>(0));
            Assert.True(store.Has<double>(0));
        }

        [Fact]
        public void ValueToReference_EvictsOldData() {
            var store = CreateStore();
            store.Set(0, 42);
            store.Set(0, "hello");

            Assert.Equal("hello", store.Get<string>(0));
            Assert.Equal("hello", store.GetAny(0));
            Assert.Equal(0, store.Get<int>(0));
            Assert.False(store.Has<int>(0));
            Assert.True(store.Has<string>(0));
        }

        [Fact]
        public void ReferenceToValue_EvictsOldData() {
            var store = CreateStore();
            store.Set(0, "hello");
            store.Set(0, 42);

            Assert.Equal(42, store.Get<int>(0));
            Assert.Equal(42, store.GetAny(0));
            Assert.Null(store.Get<string>(0));
            Assert.False(store.Has<string>(0));
            Assert.True(store.Has<int>(0));
        }

        [Fact]
        public void SameType_Overwrite_KeepsRegistryConsistent() {
            var store = CreateStore();
            store.Set(0, 42);
            store.Set(0, 99);

            Assert.Equal(typeof(int), store.TypeOf(0));
            Assert.Equal(99, store.Get<int>(0));
            Assert.True(store.Has<int>(0));
        }
    }

    public class TryGet {
        [Fact]
        public void ValueType_Present_ReturnsTrueAndValue() {
            var store = CreateStore();
            store.Set(0, 42);
            Assert.True(store.TryGet(0, out int value));
            Assert.Equal(42, value);
        }

        [Fact]
        public void ValueType_Absent_ReturnsFalse() {
            var store = CreateStore();
            Assert.False(store.TryGet(5, out int value));
            Assert.Equal(0, value);
        }

        [Fact]
        public void ValueType_WrongType_ReturnsFalse() {
            var store = CreateStore();
            store.Set(0, 42);
            Assert.False(store.TryGet(0, out double value));
            Assert.Equal(0, value);
        }

        [Fact]
        public void ValueType_OutOfRange_ReturnsFalse() {
            var store = CreateStore();
            Assert.False(store.TryGet(BucketSize, out int value));
            Assert.Equal(0, value);
        }

        [Fact]
        public void ValueType_UnregisteredType_ReturnsFalse() {
            var store = CreateStore();
            Assert.False(store.TryGet(0, out float value));
            Assert.Equal(0, value);
        }

        [Fact]
        public void ReferenceType_Present_ReturnsTrueAndValue() {
            var store = CreateStore();
            store.Set(0, "hello");
            Assert.True(store.TryGet(0, out string? value));
            Assert.Equal("hello", value);
        }

        [Fact]
        public void ReferenceType_Absent_ReturnsFalse() {
            var store = CreateStore();
            Assert.False(store.TryGet(0, out string? value));
            Assert.Null(value);
        }
    }

    public class GetAny {
        [Fact]
        public void ValueType_Present_ReturnsValue() {
            var store = CreateStore();
            store.Set(0, 42);
            Assert.Equal(42, store.GetAny(0));
        }

        [Fact]
        public void ReferenceType_Present_ReturnsValue() {
            var store = CreateStore();
            store.Set(0, "hello");
            Assert.Equal("hello", store.GetAny(0));
        }

        [Fact]
        public void Absent_ReturnsNull() {
            var store = CreateStore();
            Assert.Null(store.GetAny(5));
        }

        [Fact]
        public void OutOfRange_ReturnsNull() {
            var store = CreateStore();
            Assert.Null(store.GetAny(BucketSize));
            Assert.Null(store.GetAny(-1));
        }

        [Fact]
        public void TryGetAny_Absent_ReturnsFalse() {
            var store = CreateStore();
            Assert.False(store.TryGetAny(5, out var value));
            Assert.Null(value);
        }

        [Fact]
        public void TryGetAny_Present_ReturnsTrueAndValue() {
            var store = CreateStore();
            store.Set(0, 42);
            Assert.True(store.TryGetAny(0, out var value));
            Assert.Equal(42, value);
        }
    }

    public class Has {
        [Fact]
        public void ExactType_Present_ReturnsTrue() {
            var store = CreateStore();
            store.Set(0, 42);
            Assert.True(store.Has<int>(0));
        }

        [Fact]
        public void DifferentType_ReturnsFalse() {
            var store = CreateStore();
            store.Set(0, 42);
            Assert.False(store.Has<double>(0));
        }

        [Fact]
        public void Absent_ReturnsFalse() {
            var store = CreateStore();
            Assert.False(store.Has<int>(5));
        }

        [Fact]
        public void OutOfRange_ReturnsFalse() {
            var store = CreateStore();
            Assert.False(store.Has<int>(BucketSize));
            Assert.False(store.Has<int>(-1));
        }

        [Fact]
        public void UnregisteredValueType_ReturnsFalse() {
            var store = CreateStore();
            Assert.False(store.Has<float>(0));
        }

        [Fact]
        public void HasAny_Written_ReturnsTrue() {
            var store = CreateStore();
            store.Set(0, 42);
            Assert.True(store.HasAny(0));
        }

        [Fact]
        public void HasAny_Absent_ReturnsFalse() {
            var store = CreateStore();
            Assert.False(store.HasAny(5));
        }

        [Fact]
        public void HasAny_OutOfRange_ReturnsFalse() {
            var store = CreateStore();
            Assert.False(store.HasAny(BucketSize));
            Assert.False(store.HasAny(-1));
        }
    }

    public class TypeOfAndCanCast {
        [Fact]
        public void TypeOf_ReturnsRegisteredType() {
            var store = CreateStore();
            store.Set(0, 42);
            Assert.Equal(typeof(int), store.TypeOf(0));
        }

        [Fact]
        public void TypeOf_ReferenceType_ReturnsRegisteredType() {
            var store = CreateStore();
            store.Set(0, "hello");
            Assert.Equal(typeof(string), store.TypeOf(0));
        }

        [Fact]
        public void TypeOf_AbsentOrOutOfRange_ReturnsNull() {
            var store = CreateStore();
            Assert.Null(store.TypeOf(5));
            Assert.Null(store.TypeOf(BucketSize));
            Assert.Null(store.TypeOf(-1));
        }

        [Fact]
        public void CanCast_AssignableType_ReturnsTrue() {
            var store = CreateStore();
            store.Set(0, 42);
            Assert.True(store.CanCast<int>(0));
            Assert.True(store.CanCast<object>(0));
        }

        [Fact]
        public void CanCast_ReferenceType_ReturnsTrue() {
            var store = CreateStore();
            store.Set(0, "hello");
            Assert.True(store.CanCast<string>(0));
            Assert.True(store.CanCast<object>(0));
        }

        [Fact]
        public void CanCast_NonAssignableType_ReturnsFalse() {
            var store = CreateStore();
            store.Set(0, 42);
            Assert.False(store.CanCast<double>(0));
            Assert.False(store.CanCast<string>(0));
        }

        [Fact]
        public void CanCast_AbsentOrOutOfRange_ReturnsFalse() {
            var store = CreateStore();
            Assert.False(store.CanCast<int>(5));
            Assert.False(store.CanCast<int>(BucketSize));
            Assert.False(store.CanCast<int>(-1));
        }
    }

    public class Concurrency {
        [Fact]
        public void DistinctKeyWrites_AllConsistent() {
            var store = CreateStore();
            Parallel.For(0, BucketSize, i => store.Set(i, i));
            for (var i = 0; i < BucketSize; i++) {
                Assert.Equal(i, store.Get<int>(i));
            }
        }

        [Fact]
        public void MixedOperations_DoNotThrow() {
            var store = CreateStore();
            var exceptions = new ConcurrentQueue<Exception>();

            Parallel.For(0, 500, i => {
                try {
                    var key = i % BucketSize;
                    store.Set(key, i);
                    store.Get<int>(key);
                    store.GetAny(key);
                    store.Has<int>(key);
                    store.HasAny(key);
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

        [Fact]
        public void SameKeyRetyping_DoesNotCorruptOrThrow() {
            var store = CreateStore();
            var exceptions = new ConcurrentQueue<Exception>();

            Parallel.For(0, 500, i => {
                try {
                    if (i % 2 == 0) store.Set(0, i);
                    else store.Set(0, (double) i);
                } catch (Exception ex) {
                    exceptions.Enqueue(ex);
                }
            });

            Assert.Empty(exceptions);
            Assert.True(store.HasAny(0));
            Assert.True(store.TypeOf(0) == typeof(int) || store.TypeOf(0) == typeof(double));
        }
    }
}
