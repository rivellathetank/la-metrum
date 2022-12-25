using System.Text;

namespace LaMetrum {
  public static class Strings {
    static readonly char[] SpecialChars = new char[] { '\\', '"' };

    public static void Quote(string s, StringBuilder buf) {
      buf.Append('"');
      Escape(s, '\\', SpecialChars, buf);
      buf.Append('"');
    }

    public static string Quote(string s) {
      StringBuilder buf = new(s.Length + 2);
      Quote(s, buf);
      return buf.ToString();
    }

    public static bool IsWhitespace(string s) => s is not null && string.IsNullOrWhiteSpace(s);

    static void Escape(string s, char escape, char[] chars, StringBuilder sb) {
      int start = 0;
      while (true) {
        int next = s.IndexOfAny(chars, start);
        if (next < 0) break;
        sb.Append(s, start, next - start);
        sb.Append(escape);
        sb.Append(s[next]);
        start = next + 1;
      }
      sb.Append(s, start, s.Length - start);
    }
  }
}
