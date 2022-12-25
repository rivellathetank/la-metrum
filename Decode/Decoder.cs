using IronSnappy;
using K4os.Compression.LZ4;
using NLog;

namespace LaMetrum {
  class Decoder {
    enum Compression : byte {
      None = 0,
      LZ4 = 1,
      Snappy = 2,
      Oodle = 3,
    }

    const int MaxDataLength = 0x11FF2;

    static readonly Logger _log = LogManager.GetCurrentClassLogger();

    int _len = 0;
    byte[] _input = new byte[64 << 10];
    readonly byte[] _chunk = new byte[MaxDataLength];

    public void Reset() {
      if (_len > 0) Drop(_len, "stream terminated abnormally");
    }

    public IEnumerable<RawMessage> Decode(ArraySegment<byte> input) {
      if (_len != 0) {
        if (_len + input.Count > _input.Length) Array.Resize(ref _input, _len + input.Count);
        Array.Copy(input.Array, input.Offset, _input, _len, input.Count);
        input = new ArraySegment<byte>(_input, 0, _len + input.Count);
      }

      while (input.Count >= 6) {
        if (input[5] != 1) {
          Drop(input.Count, $"wrong packet header: {input[5]} != 1");
          yield break;
        }

        ushort n = BitConverter.ToUInt16(input.Array, input.Offset);
        if (n < 6) {
          Drop(input.Count, $"packet too short: {n} < 6");
          yield break;
        }
        if (n > input.Count) break;

        OpCode op = (OpCode)BitConverter.ToUInt16(input.Array, input.Offset + 2);
        Compression compression = (Compression)input[4];
        ArraySegment<byte> chunk = input.Slice(6, n - 6);

        if (chunk.Count > MaxDataLength) {
          Drop(input.Count, $"data too long: {chunk.Count} > {MaxDataLength}");
          yield break;
        }

        if (Enum.IsDefined(op)) {
          if (ReferenceEquals(input.Array, _input)) {
            Xor(chunk, chunk, (int)op);
          } else {
            ArraySegment<byte> buf = new(_input, 0, chunk.Count);
            Xor(chunk, buf, (int)op);
            chunk = buf;
          }

          switch (compression) {
            case Compression.None:
              break;
            case Compression.LZ4:
              int len = LZ4Codec.Decode(chunk.Array, chunk.Offset, chunk.Count, _chunk, 0, _chunk.Length);
              if (len < 0) {
                Drop(input.Count, $"LZ4 decoding failed: {len}");
                yield break;
              }
              chunk = new(_chunk, 0, len);
              break;
            case Compression.Snappy:
              try {
                chunk = Snappy.Decode(chunk);
              } catch (Exception e) {
                Drop(input.Count, $"Snappy decoding failed: {e.Message}");
                yield break;
              }
              break;
            case Compression.Oodle:
              try {
                chunk = new(_chunk, 0, Oodle.Decompress(chunk, _chunk));
              } catch (Exception e) {
                Drop(input.Count, $"Oodle decoding failed: {e.Message}");
                yield break;
              }
              break;
            default:
              Drop(input.Count, $"unknwon compression method: {compression}");
              yield break;
          }

          if (chunk.Count < 16) {
            Drop(input.Count, $"data too short: {chunk.Count} < 16");
            yield break;
          }

          chunk = chunk.Slice(16);
          _log.Debug("Decoded {0}B with {1} into {2} with {3}B of data", n, compression, op, chunk.Count);
          yield return new RawMessage(input.Count, op, chunk);
        }

        input = input.Slice(n);
      }

      if (_input.Length < input.Count) _input = new byte[input.Count];
      Array.Copy(input.Array, input.Offset, _input, 0, input.Count);
      _len = input.Count;
    }

    public void Drop(int unprocessedBytes, string reason) {
      _log.Warn("dropping {0} byte(s) on the floor: {1}", unprocessedBytes, reason);
      _len = 0;
    }

    static void Xor(in ArraySegment<byte> src, in ArraySegment<byte> dst, int seed) {
      Check(src.Count == dst.Count);
      Check(Resource.XorTable.Length == 256, Resource.XorTable.Length);
      for (int i = 0; i != src.Count; ++i) {
        dst[i] = (byte)(src[i] ^ Resource.XorTable[(byte)seed++]);
      }
    }
  }
}
