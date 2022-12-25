namespace LaMetrum {
  static class Bytes {
    public static ushort Reverse16(ushort x) => (ushort)(x >> 8 | x << 8);

    public static uint Reverse32(uint x) {
      x = (x >> 16) | (x << 16);
      return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
    }
  }
}
