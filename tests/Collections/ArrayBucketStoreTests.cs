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
            Assert.Null(store.GetAny(0, typeof(object)));
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
        public void MultipleTypes_SameKey_StoredIndependently() {
            var store = CreateStore();
            store.Set(0, 42);
            store.Set(0, 3.14);

            Assert.Equal(42, store.Get<int>(0));
            Assert.Equal(3.14, store.Get<double>(0));
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
        }

        [Fact]
        public void StoresReferenceType() {
            var store = CreateStore();
            store.Set(1, "hello", typeof(string));
            Assert.Equal("hello", store.Get<string>(1));
        }

        [Fact]
        public void StoresNullReference() {
            var store = CreateStore();
            store.Set(2, null, typeof(string));
            Assert.True(store.HasAny(2));
            Assert.Null(store.GetAny(2, typeof(string)));
        }
    }

    public class GetAny {
        [Fact]
        public void ValueType_Present_ReturnsBoxedValue() {
            var store = CreateStore();
            store.Set(0, 42);
            Assert.Equal(42, store.GetAny(0, typeof(int)));
        }

        [Fact]
        public void ReferenceType_Present_ReturnsValue() {
            var store = CreateStore();
            store.Set(0, "hello");
            Assert.Equal("hello", store.GetAny(0, typeof(string)));
        }

        [Fact]
        public void Absent_ReturnsNull() {
            var store = CreateStore();
            Assert.Null(store.GetAny(5, typeof(int)));
        }

        [Fact]
        public void OutOfRange_ReturnsNull() {
            var store = CreateStore();
            Assert.Null(store.GetAny(BucketSize, typeof(int)));
            Assert.Null(store.GetAny(-1, typeof(int)));
        }

        [Fact]
        public void WrongType_ReturnsNullForUnregisteredType() {
            var store = CreateStore();
            store.Set(0, 42);
            Assert.Null(store.GetAny(0, typeof(float)));
        }
    }

    public class HasAny {
        [Fact]
        public void Written_ReturnsTrue() {
            var store = CreateStore();
            store.Set(0, 42);
            Assert.True(store.HasAny(0));
        }

        [Fact]
        public void Absent_ReturnsFalse() {
            var store = CreateStore();
            Assert.False(store.HasAny(5));
        }

        [Fact]
        public void OutOfRange_ReturnsFalse() {
            var store = CreateStore();
            Assert.False(store.HasAny(BucketSize));
            Assert.False(store.HasAny(-1));
        }

        [Fact]
        public void NullReference_StillPresent() {
            var store = CreateStore();
            store.Set(0, null, typeof(string));
            Assert.True(store.HasAny(0));
        }
    }

    public class Clear {
        [Fact]
        public void ClearsPresenceAndData() {
            var store = CreateStore();
            store.Set(0, 42);
            Assert.True(store.HasAny(0));

            store.Clear(0);

            Assert.False(store.HasAny(0));
            Assert.Equal(0, store.Get<int>(0));
        }

        [Fact]
        public void OutOfRange_DoesNotThrow() {
            var store = CreateStore();
            store.Clear(-1);
            store.Clear(BucketSize);
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
                    store.GetAny(key, typeof(int));
                    store.HasAny(key);
                } catch (Exception ex) {
                    exceptions.Enqueue(ex);
                }
            });

            Assert.Empty(exceptions);
        }
    }
}
