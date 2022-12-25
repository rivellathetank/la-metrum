using NLog;
using System.Numerics;
using System.Reflection;

using E = System.Linq.Expressions.Expression;

namespace LaMetrum {
  public class FieldReader {
    const int MaxStringLength = 256;
    const int MaxListLength = 256;
    const int MaxByteArrayLength = 1024;

    static readonly Logger _log = LogManager.GetCurrentClassLogger();

    readonly byte[] _data;
    readonly int _from;
    readonly int _to;

    int _pos;

    public FieldReader(in ArraySegment<byte> data) {
      _data = data.Array;
      _from = data.Offset;
      _to = _from + data.Count;
      _pos = data.Offset;
    }

    public int RemainingBytes => _to - _pos;

    public void Discard() => Skip(RemainingBytes);

    public long ReadPackedInt() {
      byte x = ReadByte();
      return (long)(ReadLE((x >> 1) & 7) << 4 | (ulong)x >> 4) * (-2 * (x & 1) + 1);
    }

    /*
    public ulong ReadSimpleInt(bool expectTwoBytes, string source) {
      ulong x = ReadLE(2);
      ulong y = x & 0xFFF;
      if (y < 0x81F) {
        if (expectTwoBytes) _log.Warn("ReadSimpleInt() read 8 bytes when 2 were expected by {0}: 0x{1:X}", source, x);
        return ReadLE(6) << 16 | y;
      } else {
        if (!expectTwoBytes) _log.Warn("ReadSimpleInt() read 2 bytes when 8 were expected by {0}: 0x{1:X}", source, x);
        return y | 0x11000UL;
      }
    }
    */

    public ulong ReadFlag() {
      byte x = ReadByte();
      Skip(4 * BitOperations.PopCount(x & 0b111111U));
      if ((x & 0x40) != 0) Skip(ReadUInt16());
      return 0;
    }

    // This method has incompatible result type compared to LostArkLogger.
    public byte[][] ReadPackedValues(int n0, int n1, int n2, int n3, int n4, int n5, int n6) {
      byte x = ReadByte();
      return new byte[][] {
        (x & (1 << 0)) == 0 ? null : ReadBytes(n0),
        (x & (1 << 1)) == 0 ? null : ReadBytes(n1),
        (x & (1 << 2)) == 0 ? null : ReadBytes(n2),
        (x & (1 << 3)) == 0 ? null : ReadBytes(n3),
        (x & (1 << 4)) == 0 ? null : ReadBytes(n4),
        (x & (1 << 5)) == 0 ? null : ReadBytes(n5),
        (x & (1 << 6)) == 0 ? null : ReadBytes(n6),
      };
    }

    public string ReadString() {
      ushort n = ReadUInt16();
      if (n > MaxStringLength) throw new Exception($"string field too long: {n} > {MaxStringLength}");
      char[] chars = new char[n];
      for (int i = 0; i != n; ++i) chars[i] = (char)ReadUInt16();
      if (!IsValidUnicode(new ArraySegment<char>(chars, 0, n))) {
        throw new Exception(
            $"Invalid UNICODE string: [{string.Join(", ", chars.Take(n).Select(c => (ushort)c))}]");
      }
      return new string(chars, 0, n);
    }

    public T Read<T>() => Factory<T>.Instance.Invoke(this);

    // T must be byte[]. This API quirck from LostArkLogger and cannot be fixed without
    // breaking source-code compatibility with message definitions.
    public List<byte[]> ReadList<T>(int elemSize) {
      Check(typeof(T) == typeof(byte[]), typeof(T));
      return ReadListImpl((self: this, elemSize), static (x) => x.self.ReadBytes(x.elemSize));
    }

    public List<T> ReadList<T>() {
      Check(typeof(T) != typeof(byte[]));
      return ReadListImpl(this, static (self) => Factory<T>.Instance.Invoke(self));
    }

    public byte ReadByte() {
      CheckAvailability(1);
      return _data[_pos++];
    }

    public byte[] ReadBytes(int count) {
      CheckAvailability(count, MaxByteArrayLength);
      byte[] res = new byte[count];
      Array.Copy(_data, _pos, res, 0, count);
      _pos += count;
      return res;
    }

    public ushort ReadUInt16() => (ushort)ReadLE(2);
    public uint ReadUInt32() => (uint)ReadLE(4);
    public ulong ReadUInt64() => ReadLE(8);

    public void Seek(int n) {
      long p = _pos + n;
      if (p < _from) throw new Exception($"field underflows its message: {-n} > {_pos} - {_from}");
      if (p > _to) throw new Exception($"field overflows its message: {n} > {_to} - {_pos}");
      _pos = (int)p;
    }

    public void Skip(int n) {
      CheckAvailability(n);
      _pos += n;
    }

    void CheckAvailability(int n) {
      Check(n >= 0, n);
      if (n > _to - _pos) throw new Exception($"field overflows its message: {n} > {_to} - {_pos}");
    }

    void CheckAvailability(int n, int max) {
      CheckAvailability(n);
      if (n > max) throw new Exception($"field too long: {n} > {max}");
    }

    ulong ReadLE(int n) {
      Check(n <= 8);
      CheckAvailability(n);
      ulong res = 0;
      for (int i = 0; i != n; ++i) res |= (ulong)_data[_pos++] << 8 * i;
      return res;
    }

    static bool IsValidUnicode(in ArraySegment<char> chars) {
      for (int i = 0; i != chars.Count; ++i) {
        char c = chars[i];
        if (c < 32 || c == 127 || c == 0xFFFE || c == 0xFEFF) return false;
        if (c >= 0xD800 && c <= 0xDBFF) {
          if (i == chars.Count) return false;
          char next = chars[i + 1];
          if (next < 0xDC00 || next > 0xDFFF) return false;
          ++i;
        } else if (c >= 0xDC00 && c <= 0xDFFF) {
          return false;
        }
      }
      return true;
    }

    List<T> ReadListImpl<T, A>(A a, Func<A, T> f) {
      ushort n = ReadUInt16();
      if (n > MaxListLength) throw new Exception($"list field too long: {n} > {MaxListLength}");
      List<T> res = new(n);
      for (int i = 0; i != n; ++i) res.Add(f.Invoke(a));
      return res;
    }

    static class Factory<T> {
      static Factory() {
        Instance = default(T) switch {
          byte => Cast((r) => r.ReadByte()),
          ushort => Cast((r) => r.ReadUInt16()),
          uint => Cast((r) => r.ReadUInt32()),
          ulong => Cast((r) => r.ReadUInt64()),
          _ => Cast(Ctor()),
        };

        static Func<FieldReader, T> Cast<U>(Func<FieldReader, U> f) => (Func<FieldReader, T>)(object)f;
      }

      public static Func<FieldReader, T> Instance { get; }

      static Func<FieldReader, T> Ctor() {
        Type t = typeof(T);
        Check(t.IsClass && !t.IsAbstract);
        var r = E.Parameter(typeof(FieldReader), "reader");
        ConstructorInfo c = t.GetConstructor(new Type[] { typeof(FieldReader) });
        if (c is null) throw new Exception($"class {t.Name} is missing a constructor from {nameof(FieldReader)}");
        return E.Lambda<Func<FieldReader, T>>(E.New(c, r), r).Compile();
      }
    }
  }
}
