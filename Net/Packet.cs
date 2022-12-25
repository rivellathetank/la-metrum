using NLog;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LaMetrum {
  class Packet {
    static readonly Logger _log = LogManager.GetCurrentClassLogger();

    readonly byte[] _buf = new byte[64 << 10];
    readonly ushort _srcPort;
    int _offset;
    int _count;

    public Packet(short srcPort, uint dstIP) {
      _srcPort = Bytes.Reverse16((ushort)srcPort);
      DstIP = dstIP;
    }

    public Memory<byte> Buf => new(_buf);
    public ArraySegment<byte> Data => new(_buf, _offset, _count);

    public uint SrcIP { get; private set; }
    public uint DstIP { get; }
    public ushort DstPort { get; private set; }

    public uint SeqNum { get; private set; }
    public bool SYN => _count == 0;

    public bool Decode(int count) => DecodeIPv4(count) && DecodeTCP();

    unsafe bool DecodeIPv4(int count) {
      Check(count <= _buf.Length);
      if (count < sizeof(IPv4Header)) return false;

      fixed (byte* p = _buf) {
        ref IPv4Header h = ref Unsafe.AsRef<IPv4Header>(p);
        if ((h.Version_IHL >> 4) != 4) return false;  // IPv4
        if (h.Protocol != 6) return false;  // TCP
        if (h.DstIP != DstIP) return false;
        int packetLen = Bytes.Reverse16(h.PacketLength);
        _offset = (h.Version_IHL & 0x0F) << 2;
        if (_offset < sizeof(IPv4Header)) return false;
        if (_offset > count) return false;
        if (packetLen != count) return false;
        // This includes two checks: MF flag is not set and fragment offset is zero.
        if ((h.Flags_FragmentOffset & (0x20 | 0xFF1F)) != 0) return false;
        Checksum checksum = new();
        checksum.AddMany(_buf.AsSpan(0, _offset));
        if (checksum.Get() != 0) {
          _log.Warn("IPv4 checksum mismatch");
          return false;
        }
        SrcIP = h.SrcIP;
        _count = count - _offset;
        return true;
      }
    }

    unsafe bool DecodeTCP() {
      Check(_offset > 0);
      if (_count < sizeof(TCPHeader)) return false;

      fixed (byte* p = _buf) {
        ref TCPHeader h = ref Unsafe.AsRef<TCPHeader>(p + _offset);
        if (h.SrcPort != _srcPort) return false;
        int headerLen = h.DataOffset_NS >> 4 << 2;
        if (headerLen < sizeof(TCPHeader)) return false;
        bool syn = (h.Flags & 2) != 0;
        if (headerLen == _count) {
          if (!syn) return false;
        } else {
          if (headerLen > _count) return false;
          if (syn) return false;
        }
        if (h.Checksum != 0) {
          Checksum checksum = new();
          checksum.Add32(SrcIP);
          checksum.Add32(DstIP);
          checksum.Add16(6 << 8);
          checksum.Add16(Bytes.Reverse16((ushort)_count));
          checksum.AddMany(_buf.AsSpan(_offset, _count));
          if (checksum.Get() != 0) {
            _log.Warn("TCP checksum mismatch: {0}",
                      string.Join("", _buf.Skip(_offset).Take(_count).Select(x => $"{x:X02}")));
            return false;
          }
        }
        _count -= headerLen;
        _offset += headerLen;
        SeqNum = Bytes.Reverse32(h.SeqNum);
        DstPort = Bytes.Reverse16(h.DstPort);
        return true;
      }
    }

    struct Checksum {
      ulong _sum;

      public void Add16(ushort x) => _sum += x;
      public void Add32(uint x) => AddMany(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref x, 1)));

      public void AddMany(ReadOnlySpan<byte> bytes) {
        ReadOnlySpan<ushort> words = MemoryMarshal.Cast<byte, ushort>(bytes);
        for (int i = 0; i != words.Length; ++i) _sum += words[i];
        if ((bytes.Length & 1) != 0) _sum += bytes[^1];
      }

      public ushort Get() {
        ulong res = (_sum >> 16) + (_sum & 0xFFFF);
        res += res >> 16;
        return (ushort)~res;
      }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IPv4Header {
      public byte Version_IHL;
      public byte TOS_ECN;
      public ushort PacketLength;
      public ushort Identification;
      public ushort Flags_FragmentOffset;
      public byte TTL;
      public byte Protocol;
      public ushort Checksum;
      public uint SrcIP;
      public uint DstIP;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TCPHeader {
      public ushort SrcPort;
      public ushort DstPort;
      public uint SeqNum;
      public uint AckNum;
      public byte DataOffset_NS;
      public byte Flags;
      public ushort WindowSize;
      public ushort Checksum;
      public ushort Urgent;
    }
  }
}
