using System.Net;
using System.Net.Sockets;
using System.Text;

var listener = new TcpListener(IPAddress.Loopback, 2740);
listener.Start();
Console.WriteLine($"Now listening on: {listener.LocalEndpoint}");

ReadOnlySpan<byte> httpHeaderTerminator = "\r\n\r\n"u8;
byte[] header = Encoding.ASCII.GetBytes(
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/plain; charset=utf-8\r\n" +
                "Transfer-Encoding: chunked\r\n" +
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

    for (int position = 3; position >= 1; position--)
    {
        WriteChunk(stream, Encoding.UTF8.GetBytes($"Vị trí của bạn trong hàng chờ: {position}\n"));
        Thread.Sleep(1000); //simulate mỗi s response vị trí mới trong hàng chờ
    }
    WriteChunk(stream, Encoding.UTF8.GetBytes("Đến lượt bạn! Mời chọn vé.\n"));
    stream.Write("0\r\n\r\n"u8);
}

static void WriteChunk(NetworkStream stream,  byte[] data)
{
    stream.Write(Encoding.ASCII.GetBytes($"{data.Length:X}\r\n"));
    stream.Write(data);
    stream.Write("\r\n"u8);
}