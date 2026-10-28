using System.Net;
using System.Net.Sockets;

var listener = new TcpListener(IPAddress.Loopback, 2740);
listener.Start();
Console.WriteLine($"Now listening on: {listener.LocalEndpoint}");

while (true)
{
    using var client = listener.AcceptTcpClient();
    Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");
}
