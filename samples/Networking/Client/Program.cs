
using LiteNetLib;
using Shiron.Networking;
using Shiron.Samples.Networking.Common;

Console.Write("Enter Username: ");
string? username = Console.ReadLine();
if (string.IsNullOrWhiteSpace(username)) {
    System.Console.WriteLine("Invalid username.");
    return;
}

Console.Write("Enter server IP address: ");
string? serverIP = Console.ReadLine();
if (string.IsNullOrWhiteSpace(serverIP)) {
    System.Console.WriteLine("Invalid server IP address.");
    return;
}

var serverAddress = new {
    IP = serverIP[..serverIP.LastIndexOf(':')],
    Port = uint.Parse(serverIP[(serverIP.LastIndexOf(':') + 1)..])
};

var common = new Common();
common.Init();

var client = new NetworkClient(common.Registry);

client.OnConnected += () => Console.WriteLine("Connected to server!");
client.OnDisconnected += () => Console.WriteLine("Disconnected from server.");

client.OnPacketReceived += (packet) => {
    if (packet is CommandMessage chat) {
        Console.WriteLine($"\n> {chat.Sender}: {chat.Message}");
    }
};

// Ignore warning as we want to run this without awaiting.
#pragma warning disable CS4014
Task.Run(async () => {
    client.Connect(serverAddress.IP, serverAddress.Port);

    while (true) {
        client.Poll();
        await Task.Delay(15);
    }
});

// 4. Input Loop (Main Thread)
while (true) {
    string? msg = Console.ReadLine();
    if (msg == null) continue;

    if (!string.IsNullOrWhiteSpace(msg) && client.IsConnected) {
        // Create and Send
        var packet = new CommandMessage {
            Sender = username,
            Message = msg
        };

        client.Send(packet, DeliveryMethod.ReliableOrdered);
    }
}
