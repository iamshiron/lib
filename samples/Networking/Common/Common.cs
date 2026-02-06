using System.Diagnostics.Contracts;
using MemoryPack;
using Shiron.Lib.Networking;

namespace Shiron.Samples.Networking.Common;

[MemoryPackable]
public partial class CommandPing : INetworkCommand {
    public int SequenceID { get; set; }
    public long Timestamp { get; set; }
}
[MemoryPackable]
public partial class CommandMessage : INetworkCommand {
    public int SequenceID { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public static class OpCodes {
    public const ushort Ping = 0x1000;
    public const ushort Message = 0x1001;
}

public class Common {
    public CommandRegistry Registry { get; } = new CommandRegistry();

    public void Init() {
        Registry.Register<CommandPing>(OpCodes.Ping);
        Registry.Register<CommandMessage>(OpCodes.Message);
    }
}
