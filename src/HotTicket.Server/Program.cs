using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
    builder
        .SetMinimumLevel(LogLevel.Debug)
        .AddSimpleConsole(options =>
        {
            options.TimestampFormat = "HH:mm:ss.fff ";
            options.SingleLine = true;
        }));
ILogger logger = loggerFactory.CreateLogger("HotTicket.Server");

var listener = new TcpListener(IPAddress.Loopback, 2740);
listener.Start();
logger.LogInformation("Now listening on {Endpoint}", listener.LocalEndpoint);

ReadOnlySpan<byte> httpHeaderTerminator = "\r\n\r\n"u8;
ReadOnlySpan<byte> body = "hello"u8;
byte[] rawResponseHeader = Encoding.ASCII.GetBytes(
                "HTTP/1.1 200 OK\r\n" +
                "Connection: close\r\n" +
               $"Content-Length: {body.Length}\r\n" +
                "\r\n");

byte[] buffer = new byte[256];
while (true)
{
    using var client = listener.AcceptTcpClient();
    var remoteEndPoint = client.Client.RemoteEndPoint;
    logger.LogInformation("Client connected {RemoteEndPoint}", remoteEndPoint);

    NetworkStream stream = client.GetStream();

    List<byte> rawRequestHeader = [];
    int idx = -1;
    int numberOfBytesRead;
    try
    {
        while ((numberOfBytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
        {
            /* Each time it is called, Read() only overwrites the first numberOfBytesRead cells.
             * The subsequent cells retain the bytes from the previous read operation.

                Read      numberOfBytesRead    Buffer content     New bytes
                1	          4	                 A B C D            A B C D
                2	          2	                 E F C D            E F
            */
            rawRequestHeader.AddRange(buffer.AsSpan(0, numberOfBytesRead));
            idx = CollectionsMarshal.AsSpan(rawRequestHeader).IndexOf(httpHeaderTerminator);
            if (idx != -1) break;
        }
        if (idx == -1)
        {
            logger.LogWarning("Client {RemoteEndPoint} closed before the request headers were complete", remoteEndPoint);
            continue;
        }

        logger.LogDebug("Request headers from {RemoteEndPoint}:\n{Headers}",
            remoteEndPoint,
            Encoding.ASCII.GetString(CollectionsMarshal.AsSpan(rawRequestHeader)[..idx]));

        stream.Write(rawResponseHeader);
        stream.Write(body);
    }
    catch (IOException ioExc)
    {
        logger.LogError(ioExc, "Connection error with {RemoteEndPoint}", remoteEndPoint);
    }
}