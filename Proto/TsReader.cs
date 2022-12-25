namespace LaMetrum {
  readonly struct TsReader {
    readonly FieldReader _reader;

    public TsReader(FieldReader reader) => _reader = reader;

    public short i16() => (short)u16();
    public int i32() => (int)u32();
    public long i64() => (long)u64();

    public byte u8() => _reader.ReadByte();
    public ushort u16() => _reader.ReadUInt16();
    public uint u32() => _reader.ReadUInt32();
    public ulong u64() => _reader.ReadUInt64();

    public long ReadNBytesInt64() => _reader.ReadPackedInt();

    public string str(int _ = 0) => _reader.ReadString();

    public byte[] bytes(int n) => _reader.ReadBytes(n);
    public byte[] bytes(uint n) => bytes((int)n);
    public byte[] bytes(uint n, uint _) => bytes(n);
    public byte[] bytes(uint n, uint _, uint m) => bytes(n * m);
    
    public bool bl() => _reader.ReadByte() == 1;

    public ushort Angle() => u16();
    public ulong Vector3F() => u64();

    public ulong LostArkDateTime() {
      ulong x = u16();
      ulong y = x & 0xFFF;
      if (y < 0x81F) {
        return y | (ulong)u16() << 16 | (ulong)u32() << 32;
      } else {
        return y | 0x11000UL;
      }
    }

    public T[] array<T>() => _reader.ReadList<T>().ToArray();

    public byte[][] ReadFlagBytes() => _reader.ReadPackedValues(1, 1, 4, 4, 4, 3, 6);

    public void array(uint n, Action f, uint _ = 0) {
      while (n-- > 0) f.Invoke();
    }
    public T[] array<T>(uint n, Func<T> f, uint _ = 0) {
      T[] res = new T[n];
      for (int i = 0; i != n; ++i) res[i] = f.Invoke();
      return res;
    }

    public void ReadFlagBytes2() {
      byte flag = u8();
      for (int i = 0; i != 6; ++i) {
        if ((flag & 1) != 0) i32();
        flag >>= 1;
      }
      if ((flag & 1) != 0) {
        int n = i16();
        if (n <= 6) skip(n);
      }
    }

    public void skip(int n) => _reader.Seek(n);

    public void Discard() => _reader.Discard();
  }
}
