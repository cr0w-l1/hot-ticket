using System.Net;
using System.Net.Sockets;
using System.Text;

var listener = new TcpListener(IPAddress.Loopback, 2740);
listener.Start();
Console.WriteLine($"Now listening on: {listener.LocalEndpoint}");

ReadOnlySpan<byte> httpHeaderTerminator = "\r\n\r\n"u8;
ReadOnlySpan<byte> body = "hello"u8;
byte[] header = Encoding.ASCII.GetBytes(
                "HTTP/1.1 200 OK\r\n" +
                "Connection: close\r\n" +
               $"Content-Length: {body.Length}\r\n" +
                "\r\n");

byte[] buffer = new byte[256];
while (true)
{
    using var client = listener.AcceptTcpClient();
    Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");

    NetworkStream stream = client.GetStream();

    int idx = -1;
    while (stream.Read(buffer, 0, buffer.Length) != 0)
    {
        idx = buffer.AsSpan().IndexOf(httpHeaderTerminator);
        if (idx != -1) break;
    }
    stream.Write(header);
    stream.Write(body);
}
