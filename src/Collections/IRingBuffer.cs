namespace Shiron.Lib.Collections;

public interface IRingBuffer : IReadOnlyRingBuffer {
    void Add(double item);
    void SyncSums();
}
