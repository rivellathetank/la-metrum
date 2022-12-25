using System.IO.Compression;
using System.Reflection;

namespace LaMetrum {
  static class Resource {
    static string RootDir { get; } = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
    static string ResourceDir { get; } = Path.Combine(RootDir, "Resources");

    public static byte[] OodleState { get; } = Read("OodleState.bin");
    public static byte[] XorTable { get; } = Read("Xor.bin");

    static byte[] Read(string fname) => File.ReadAllBytes(Path.Combine(ResourceDir, fname));

    static byte[] Decompress(byte[] input) {
      using MemoryStream strmOut = new();
      using (GZipStream gzip = new(new MemoryStream(input), CompressionMode.Decompress)) {
        gzip.CopyTo(strmOut);
      }
      return strmOut.ToArray();
    }
  }
}
