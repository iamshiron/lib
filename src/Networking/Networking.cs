
using MemoryPack;

namespace Shiron.Lib.Networking;

[MemoryPackable]
public partial class EnvelopePacket {
    public ushort TypeID { get; set; }
    public int SequenceID { get; set; }
    public byte[] Payload { get; set; } = [];
}

public interface INetworkCommand {
    int SequenceID { get; set; }
}

public class CommandRegistry {
    private Func<byte[], INetworkCommand>[] _deserializers = new Func<byte[], INetworkCommand>[256];
    private readonly Dictionary<Type, ushort> _typeToId = new();

    public void Register<T>(ushort id) where T : class, INetworkCommand {
        if (id >= _deserializers.Length)
            Array.Resize(ref _deserializers, id + 10);

        _deserializers[id] = (bytes) => {
#pragma warning disable CS8603
            return MemoryPackSerializer.Deserialize<T>(bytes);
        };
        _typeToId[typeof(T)] = id;
    }

    public INetworkCommand Deserialize(ushort id, byte[] payload) {
        if (id >= _deserializers.Length || _deserializers[id] == null)
            throw new Exception($"Unknown Command ID: {id}");

        // Fast array lookup + Delegate invoke
        return _deserializers[id](payload);
    }

    public ushort GetId<T>() => _typeToId[typeof(T)];
    public ushort GetId(Type type) => _typeToId[type];
}
