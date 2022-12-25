using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace LaMetrum {
  record struct ByteSegment(DateTime WallTime, TimeSpan Timestamp, ArraySegment<byte> Data);

  static class Sniffer {
    static readonly TimeSpan ClientPollPeriod = TimeSpan.FromSeconds(10);

    public static async IAsyncEnumerable<ByteSegment> Snif([EnumeratorCancellation] CancellationToken cancel) {
      RecvWindow recv = new();
      Packet p = new(srcPort: GameInfo.ServerPort, dstIP: Client.LocalIP);

      for (int? pid; ; await Task.Delay(ClientPollPeriod, cancel)) {
        if ((pid = Client.ClientPid()) is null) continue;

        using Socket socket = new(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
        socket.ReceiveBufferSize = 64 << 20;
        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
        socket.Bind(new IPEndPoint(p.DstIP, 0));
        Check(socket.IOControl(IOControlCode.ReceiveAll, new byte[] { 3, 0, 0, 0 }, new byte[4]) == 0);

        TCPConnection conn = Client.Connection(pid.Value);

        while (true) {
          if (!p.Decode(await socket.ReceiveAsync(p.Buf, SocketFlags.None, cancel))) continue;

          if (!Match(conn, p)) {
            if (!p.SYN) continue;
            if ((pid = Client.ClientPid()) is null) break;
            TCPConnection c = Client.Connection(pid.Value);
            if (!Match(c, p)) continue;
            conn = c;
          }

          foreach (ArraySegment<byte> data in recv.Push(p)) {
            yield return new(DateTime.UtcNow, MonotonicClock.Now, data);
          }
        }

        recv.Clear();
        yield return new(DateTime.UtcNow, MonotonicClock.Now, default);
      }
    }

    static bool Match(TCPConnection c, Packet p) => p.SrcIP == c?.RemoteIP && p.DstPort == c?.LocalPort;
  }
}
