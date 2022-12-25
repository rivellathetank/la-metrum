using NLog;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LaMetrum {
  record TCPConnection(uint LocalIP, ushort LocalPort, uint RemoteIP, ushort RemotePort);

  static class Client {
    const int AF_INET = 2;
    const int TCP_TABLE_OWNER_PID_ALL = 5;
    const int ERROR_INSUFFICIENT_BUFFER = 122;

    [StructLayout(LayoutKind.Sequential)]
    struct MIB_TCPROW_EX {
      public TcpState State;
      public uint LocalAddr;
      public int LocalPort;
      public uint RemoteAddr;
      public int RemotePort;
      public uint ProcessId;
    }

    [DllImport("iphlpapi.dll")]
    static extern int GetExtendedTcpTable(byte[] table, ref int len, bool order, uint family, int clas, uint reserved);

    public static uint LocalIP { get; } = ToNet(Route(new IPEndPoint(GameInfo.ServerIP, GameInfo.ServerPort)));

    static readonly Logger _log = LogManager.GetCurrentClassLogger();

    public static int? ClientPid() {
      Process[] processes = Process.GetProcessesByName(GameInfo.ProcessName);
      if (processes.Length == 0) {
        _log.Info("Game client is not running.");
        return null;
      }
      Check(processes.Length == 1, processes.Length);
      _log.Info("Game client PID: {0}", processes[0].Id);
      return processes[0].Id;
    }

    public static TCPConnection Connection(int pid) {
      byte[] buf = Array.Empty<byte>();

      while (true) {
        int len = buf.Length;
        int err = GetExtendedTcpTable(buf, ref len, false, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0);
        if (err == 0) break;
        Check(err == ERROR_INSUFFICIENT_BUFFER, err);
        Check(len > buf.Length);
        buf = new byte[2 * len];
      }

      unsafe {
        fixed (byte* p = buf) {
          TCPConnection res = null;
          int n = Unsafe.Read<int>(p);
          Check(n >= 0, n);

          for (int i = 0; i != n; ++i) {
            ref MIB_TCPROW_EX c = ref Unsafe.AsRef<MIB_TCPROW_EX>(p + sizeof(int) + sizeof(MIB_TCPROW_EX) * i);
            if (c.ProcessId != pid) continue;
            if (c.LocalAddr != LocalIP) continue;
            switch (c.State) {
              case TcpState.SynSent:
              case TcpState.SynReceived:
              case TcpState.Established:
                break;
              default:
                continue;
            }
            if (Bytes.Reverse16((ushort)c.RemotePort) != GameInfo.ServerPort) continue;
            Check(c.RemoteAddr != 0);
            Check(res is null);
            res = new TCPConnection(
                LocalIP: c.LocalAddr,
                LocalPort: Bytes.Reverse16((ushort)c.LocalPort),
                RemoteIP: c.RemoteAddr,
                RemotePort: Bytes.Reverse16((ushort)c.RemotePort));
          }

          return res;
        }
      }
    }

    static uint ToNet(IPAddress ip) {
      uint res = 0;
      Check(ip.TryWriteBytes(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref res, 1)), out int n));
      Check(n == 4);
      return res;
    }

    static IPAddress Route(IPEndPoint remoteEndPoint) {
      using Socket sock = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      SocketAddress addr = remoteEndPoint.Serialize();
      byte[] remoteAddr = new byte[addr.Size];
      for (int i = 0; i != addr.Size; i++) remoteAddr[i] = addr[i];

      byte[] route = new byte[remoteAddr.Length];
      int n = sock.IOControl(IOControlCode.RoutingInterfaceQuery, remoteAddr, route);
      Check(n == route.Length, n, route.Length);
      for (int i = 0; i != addr.Size; i++) addr[i] = route[i];

      return ((IPEndPoint)remoteEndPoint.Create(addr)).Address;
    }
  }
}
