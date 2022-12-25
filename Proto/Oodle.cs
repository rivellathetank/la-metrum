using NLog;
using System.Runtime.InteropServices;

namespace LaMetrum {
  class Oodle {
    const int OODLENETWORK1_DECOMP_BUF_OVERREAD_LEN = 5;

    [DllImport("kernel32")] static extern bool SetDllDirectory(string path);

    [DllImport("oo2net_9_win64")] static extern unsafe bool OodleNetwork1UDP_Decode(byte[] state, byte[] shared, byte* comp, int compLen, byte* raw, int rawLen);
    [DllImport("oo2net_9_win64")] static extern unsafe bool OodleNetwork1UDP_State_Uncompact(byte[] to, byte* from);
    [DllImport("oo2net_9_win64")] static extern unsafe void OodleNetwork1_Shared_SetWindow(byte[] data, int htbits, byte* window, int windowSize);
    [DllImport("oo2net_9_win64")] static extern int OodleNetwork1UDP_State_Size();
    [DllImport("oo2net_9_win64")] static extern int OodleNetwork1_Shared_Size(int htbits);

    static readonly Logger _log = LogManager.GetCurrentClassLogger();

    // These arrays are only read and never mutated.
    static readonly byte[] _state;
    static readonly byte[] _context;
    static readonly GCHandle _pin;

    static Oodle() {
      const int HtBits = 19;
      const int HeaderLen = 32;
      const int SampleWindowLen = 8 << 20;

      string dir = GameInfo.InstallationDir();
      Check(dir is not null);
      Check(SetDllDirectory(dir));

      _pin = GCHandle.Alloc(Resource.OodleState, GCHandleType.Pinned);
      int compStateLen = BitConverter.ToInt32(Resource.OodleState, 24);
      Check(Resource.OodleState.Length == HeaderLen + SampleWindowLen + compStateLen);
      _state = new byte[OodleNetwork1UDP_State_Size()];
      _context = new byte[OodleNetwork1_Shared_Size(HtBits)];
      
      unsafe {
        fixed (byte* p = Resource.OodleState) {
          // Reads `from` and writes `to`. Data pointed to by `from` must not be moved afterwards.
          Check(OodleNetwork1UDP_State_Uncompact(to: _state, from: p + HeaderLen + SampleWindowLen));
          // Reads `window` and writes `data`.
          OodleNetwork1_Shared_SetWindow(
              data: _context, htbits: HtBits, window: p + HeaderLen, windowSize: SampleWindowLen);
        }
      }
    }

    public static int Decompress(ArraySegment<byte> compressed, byte[] raw) {
      if (compressed.Count < 4) throw new Exception("compressed length too short: " + compressed.Count);
      int size = BitConverter.ToInt32(compressed.Array, compressed.Offset);
      if (size <= 0 || size > raw.Length) throw new Exception("invalid uncompressed length: " + size);
      compressed = compressed.Slice(4);
      if (compressed.Array.Length - compressed.Offset - compressed.Count < OODLENETWORK1_DECOMP_BUF_OVERREAD_LEN) {
        _log.Debug("oodle compressed buffer is too short; copying {0}B", compressed.Count);
        byte[] buf = new byte[compressed.Count + OODLENETWORK1_DECOMP_BUF_OVERREAD_LEN];
        Array.Copy(compressed.Array, compressed.Offset, buf, 0, compressed.Count);
        compressed = new(buf, 0, compressed.Count);
      }
      unsafe {
        fixed (byte* src = compressed.Array)
        fixed (byte* dst = raw) {
          // Writes `raw`, reads everything else.
          // Can read up to `compLen + OODLENETWORK1_DECOMP_BUF_OVERREAD_LEN` from `comp`, hence
          // the branch above to copy data if there is not enough padding.
          if (!OodleNetwork1UDP_Decode(state: _state,
                                       shared: _context,
                                       comp: src + compressed.Offset,
                                       compLen: compressed.Count,
                                       raw: dst,
                                       rawLen: size)) {
            throw new Exception("malformed compressed data");
          }
        }
      }
      return size;
    }
  }
}
