using System.Runtime.CompilerServices;

namespace LaMetrum.Stats {
  static class TableExtensions {
    public static void AddScalar<TRow>(
        this Table<TRow> table,
        string value,
        [CallerArgumentExpression("value")] string name = null) {
      if (value is not null) table.AddScalar(Name(name), value);
    }

    public static void AddCol<TRow>(
        this Table<TRow> table,
        Align align,
        Func<TRow, string> field,
        [CallerArgumentExpression("field")] string name = null) {
      table.AddCol(Name(name), align, (row) => field.Invoke(row));
    }

    static string Name(string s) {
      for (int i = s.Length - 1; i >= 0; --i) {
        char c = s[i];
        if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_') continue;
        s = s[(i + 1)..];
        break;
      }
      if (s.EndsWith("_PCT")) s = s[..^5] + '%';
      return s.Replace('_', ' ');
    }
  }

  interface ISheet {
    string KeyColumn { get; }
    ITable ToTable();
    ISheet Nested(string id);
  }
}
