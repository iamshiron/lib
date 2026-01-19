
using LiteNetLib;
using Shiron.Lib.Networking;
using Shiron.Samples.Networking.Common;

var common = new Common();
common.Init();

var server = new NetworkServer(common.Registry);
server.OnClientConnected += (id) => Console.WriteLine($"[+] Client {id} connected.");
server.OnClientDisconnected += (id) => Console.WriteLine($"[-] Client {id} disconnected.");

server.OnPacketReceived += (id, packet) => {
    if (packet is CommandMessage chat) {
        Console.WriteLine($"[{chat.Sender}]: {chat.Message}");

        // Broadcast back to everyone (Echo)
        server.Broadcast(chat, DeliveryMethod.ReliableOrdered);
    }
};

server.Start(5566);
Console.WriteLine("Server started on port 5566.");

while (true) {
    server.Poll();
    System.Threading.Thread.Sleep(15);
}
