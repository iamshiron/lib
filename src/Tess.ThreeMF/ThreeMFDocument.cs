namespace Shiron.Lib.Tess.ThreeMF;

public class ThreeMFDocument {
    public required IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Files { get; init; }
}
