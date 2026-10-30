<#
Purpose:
  Reproduce the "split terminator" bug.
  The server searched for "\r\n\r\n" only in the bytes of the last Read().
  If "\r\n\r\n" is split between two Read() calls, the server never finds it,
  so it waits forever and never answers.

How to run:
  1. Start the server.
  2. Run this script.

What this script does:
  Sends the request in two parts:
    Part 1: "GET / HTTP/1.1\r\nHost: localhost\r\n\r"  (ends in the middle of "\r\n\r\n")
    Part 2: "\n"                                        (sent 0.5 s later)
  The 0.5 s pause makes the server read part 1 before part 2 arrives,
  so the two parts come back from two different Read() calls.

Expected:
  Bug:   "No response after 5 s"
  Fixed: "Received N bytes" right after part 2 is sent
#>

$c = New-Object System.Net.Sockets.TcpClient('127.0.0.1', 2740)
$s = $c.GetStream()
$p = [Text.Encoding]::ASCII.GetBytes("GET / HTTP/1.1`r`nHost: localhost`r`n`r")
$s.Write($p, 0, $p.Length)
Start-Sleep -Milliseconds 500
$s.Write([byte[]](10), 0, 1)
"$(Get-Date -Format HH:mm:ss.fff) Sent full request, waiting..."
$s.ReadTimeout = 5000
$buf = New-Object byte[] 1024
try { $n = $s.Read($buf, 0, $buf.Length); "$(Get-Date -Format HH:mm:ss.fff) Received $n bytes" }
catch { "$(Get-Date -Format HH:mm:ss.fff) No response after 5 s" }
$c.Close()