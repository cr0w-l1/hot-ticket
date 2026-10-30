<#
Purpose:
  Check that one bad client cannot crash the server.
  A client sends half a request, then resets the connection (RST)
  while the server is waiting in Read(). Read() throws an IOException.
  If the server does not catch it, the whole server stops.

How to run:
  1. Start the server.
  2. Run this script.

What this script does:
  Step 1: Connect, send half a request, wait 0.5 s,
          then close the socket with linger 0. This sends RST.
  Step 2: Connect again as a normal client and send a full request.

Expected:
  Bug:   Step 2 prints "Server is down"
  Fixed: Step 2 prints "Server is alive"
#>

# Step 1: bad client
$bad = New-Object System.Net.Sockets.TcpClient('127.0.0.1', 2740)
$s = $bad.GetStream()
$b = [Text.Encoding]::ASCII.GetBytes("GET / HTTP/1.1`r`nHost: localhost`r`n")
$s.Write($b, 0, $b.Length)
Start-Sleep -Milliseconds 500
$bad.Client.LingerState = New-Object System.Net.Sockets.LingerOption($true, 0)
# Close the socket directly to send RST. $bad.Close() would send FIN instead.
$bad.Client.Close()
"$(Get-Date -Format HH:mm:ss.fff) Step 1: sent RST"

# Step 2: normal client
try {
    $good = New-Object System.Net.Sockets.TcpClient('127.0.0.1', 2740)
    $g = $good.GetStream()
    $g.ReadTimeout = 3000
    $r = [Text.Encoding]::ASCII.GetBytes("GET / HTTP/1.1`r`nHost: localhost`r`n`r`n")
    $g.Write($r, 0, $r.Length)
    $buf = New-Object byte[] 1024
    $n = $g.Read($buf, 0, $buf.Length)
    "$(Get-Date -Format HH:mm:ss.fff) Step 2: Server is alive (received $n bytes)"
    $good.Close()
}
catch {
    "$(Get-Date -Format HH:mm:ss.fff) Step 2: Server is down ($($_.Exception.Message))"
}