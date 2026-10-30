<#
Purpose:
  Reproduce the "stale terminator" bug.
  The server searched for "\r\n\r\n" in the whole 256-byte buffer,
  not only in the bytes read from the current client.
  Old bytes from the previous client could contain "\r\n\r\n",
  so the server answered a request that was not complete.

How to run:
  1. Start the server.
  2. Run curl once, so the buffer keeps the old curl request.
  3. Run this script right after.

What this script does:
  Sends request headers WITHOUT the final empty line ("\r\n").
  The request is not complete, so the server must not answer.

Expected:
  Bug:   "Got a response"  (the server answered too early)
  Fixed: "No response"
#>
$c = New-Object System.Net.Sockets.TcpClient('127.0.0.1', 2740)
$s = $c.GetStream()
$p = [Text.Encoding]::ASCII.GetBytes("GET / HTTP/1.1`r`nHost: x`r`n")
$s.Write($p, 0, $p.Length)
"$(Get-Date -Format HH:mm:ss.fff) Sent headers WITHOUT the final empty line"
$s.ReadTimeout = 3000
$buf = New-Object byte[] 1024
try { $n = $s.Read($buf, 0, $buf.Length); "$(Get-Date -Format HH:mm:ss.fff) Got a response: $n bytes" }
catch { "$(Get-Date -Format HH:mm:ss.fff) No response" }
$c.Close()