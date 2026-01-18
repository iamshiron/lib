
using LiteNetLib;
using LiteNetLib.Utils;
using MemoryPack;

namespace Shiron.Networking;

public interface INetworkPeer {
    void Poll();
}
public interface INetworkClient : INetworkPeer {
    bool IsConnected { get; }

    void Connect(string ip, uint port);
    void Disconnect();

    void Send<T>(T packet, DeliveryMethod method) where T : class, INetworkCommand;

    event Action OnConnected;
    event Action OnDisconnected;
    event Action<INetworkCommand> OnPacketReceived;
}
public interface INetworkServer : INetworkPeer {
    void Start(uint port);
    void Stop();

    void SendTo<T>(int connectionID, T packet, DeliveryMethod method) where T : class, INetworkCommand;
    void Broadcast<T>(T packet, DeliveryMethod method) where T : class, INetworkCommand;

    event Action<int> OnClientConnected;
    event Action<int> OnClientDisconnected;
    event Action<int, INetworkCommand> OnPacketReceived;
}

public abstract class NetworkPeer : INetEventListener {
    protected NetManager netManager;
    protected readonly NetDataWriter writer = new();
    protected readonly CommandRegistry CommandRegistry = new();

    protected NetworkPeer(CommandRegistry registry) {
        netManager = new NetManager(this) {
            AutoRecycle = true
        };
        CommandRegistry = registry;
    }

    public void Poll() => netManager.PollEvents();

    protected void SerializeAndSend<T>(NetPeer peer, T packet, DeliveryMethod method)
            where T : class, INetworkCommand {
        if (peer == null) return;

        writer.Reset();

        // 1. Write the Header (2 bytes)
        ushort id = CommandRegistry.GetId<T>();
        writer.Put(id);
        // 2. Serialize Payload directly
        // MemoryPack serializes to byte[], we write that length + bytes
        // Note: For extreme optimization, you could serialize directly into NetDataWriter
        // but MemoryPack doesn't support that natively without a custom OutputFormatter.
        // This is "Good Enough" for now.
        byte[] payload = MemoryPackSerializer.Serialize(packet);

        writer.PutBytesWithLength(payload); // Writes [Length (4b)][Data...]
        // 3. Send
        peer.Send(writer, method);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod) {
        if (reader.AvailableBytes < 2) return;
        ushort id = reader.GetUShort();

        byte[] payload = reader.GetBytesWithLength();
        INetworkCommand command = CommandRegistry.Deserialize(id, payload);
        if (command == null) return;

        HandleReceivedPacket(peer.Id, command);
    }

    protected abstract void HandleReceivedPacket(int connectionId, INetworkCommand packet);

    public abstract void OnPeerConnected(NetPeer peer);
    public abstract void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo);

    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError) {
        Console.WriteLine($"Network Error: {socketError} at {endPoint}");
    }
    public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) {
        Console.WriteLine("Received Unconnected Message");
    }
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    public void OnConnectionRequest(ConnectionRequest request) => request.Accept();
}

public class NetworkClient(CommandRegistry registry) : NetworkPeer(registry), INetworkClient {
    private NetPeer? _server;
    public bool IsConnected => _server != null && _server.ConnectionState == ConnectionState.Connected;

    public event Action? OnConnected;
    public event Action? OnDisconnected;
    public event Action<INetworkCommand>? OnPacketReceived;

    public void Connect(string ip, uint port) {
        if (!netManager.Start()) {
            throw new Exception("Failed to start network manager.");
        }
        _server = netManager.Connect(ip, (int) port, "");
    }

    public void Disconnect() {
        _server?.Disconnect();
    }

    public void Dispose() {
        netManager.Stop();
    }

    public void Send<T>(T packet, DeliveryMethod method) where T : class, INetworkCommand {
        SerializeAndSend(_server!, packet, method);
    }

    protected override void HandleReceivedPacket(int connectionId, INetworkCommand packet) {
        OnPacketReceived?.Invoke(packet);
    }

    public override void OnPeerConnected(NetPeer peer) {
        _server = peer;
        OnConnected?.Invoke();
    }

    public override void OnPeerDisconnected(NetPeer peer, DisconnectInfo info) {
        _server = null;
        OnDisconnected?.Invoke();
    }
}
public class NetworkServer(CommandRegistry registry) : NetworkPeer(registry), INetworkServer {
    public event Action<int>? OnClientConnected;
    public event Action<int>? OnClientDisconnected;
    public event Action<int, INetworkCommand>? OnPacketReceived;

    public void Start(uint port) {
        if (!netManager.Start((int) port)) {
            throw new Exception("Failed to start network manager.");
        }
    }
    public void Stop() {
        netManager.Stop();
    }

    public void SendTo<T>(int connectionId, T packet, DeliveryMethod method) where T : class, INetworkCommand {
        var peer = netManager.GetPeerById(connectionId);
        SerializeAndSend(peer, packet, method);
    }
    public void Broadcast<T>(T packet, DeliveryMethod method) where T : class, INetworkCommand {
        writer.Reset();
        ushort id = CommandRegistry.GetId<T>();
        writer.Put(id);
        byte[] payload = MemoryPackSerializer.Serialize(packet);
        writer.PutBytesWithLength(payload);
        netManager.SendToAll(writer, method);
    }

    protected override void HandleReceivedPacket(int connectionId, INetworkCommand packet) {
        OnPacketReceived?.Invoke(connectionId, packet);
    }

    public override void OnPeerConnected(NetPeer peer) {
        OnClientConnected?.Invoke(peer.Id);
    }

    public override void OnPeerDisconnected(NetPeer peer, DisconnectInfo info) {
        OnClientDisconnected?.Invoke(peer.Id);
    }
}
