using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;

namespace LaMetrum {
  class Printer {
    bool _first = true;
    bool _finished = false;
    readonly StringBuilder _buf = new("{", 128);

    public Printer Field<T>(T value, [CallerArgumentExpression("value")] string name = null) {
      Check(!_finished);
      if (value is not null) {
        if (_first) {
          _first = false;
        } else {
          _buf.Append(", ");
        }
        _buf.Append(Name(name));
        _buf.Append(" = ");
        Print(value, _buf);
      }
      return this;
    }

    public string Finish() {
      Check(!_finished);
      _finished = true;
      _buf.Append('}');
      return _buf.ToString();
    }

    public static void Print<T>(T value, StringBuilder buf) {
      if (value is null) {
        buf.Append("null");
      } else if (value is string s) {
        Strings.Quote(s, buf);
      } else if (value is DateTime t) {
        buf.AppendFormat("{0:yyyy-MM-ddTHH:mm:ss.fffZ}", t);
      } else if (value is IEnumerable e) {
        buf.Append('[');
        bool first = true;
        foreach (object x in e) {
          if (first) {
            first = false;
          } else {
            buf.Append(", ");
          }
          Print(x, buf);
        }
        buf.Append(']');
      } else {
        buf.Append(value);
      }
    }

    public static string Print<T>(T value) {
      StringBuilder buf = new();
      Print(value, buf);
      return buf.ToString();
    }

    static string Name(string s) {
      for (int i = s.Length - 1; i >= 0; --i) {
        char c = s[i];
        if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_') continue;
        return s[(i + 1)..];
      }
      return s;
    }
  }
}
