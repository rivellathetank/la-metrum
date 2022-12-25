namespace LaMetrum {
  static class Fmt {
    static readonly string[] Suffix = new[] { "", "K", "M", "G", "T", "P", "E", "Z", "Y" };

    // 42    => 42
    // 420   => 420
    // 4200  => 4.20K
    // 4.2   => 4.20
    // -4200 => -4.20K
    //
    // Invariant: SI(n).Length <= 6.
    // Invariant: SI(Math.Abs(n)).Length <= 5.
    public static string SI(double n) {
      if (n < 0) return "-" + SI(-n);

      if (n < 1000 && (int)n == n) return $"{n:F0}";

      string suf = null;
      for (int i = 0; i != Suffix.Length; ++i) {
        if (n < 1000) {
          suf = Suffix[i];
          break;
        }
        n /= 1000;
      }
      Check(suf is not null);

      return n switch {
        (>= 100) => $"{n:F0}{suf}",
        (>= 10) => $"{n:F1}{suf}",
        _ => $"{n:F2}{suf}",
      };
    }

    public static string Pct(double num, double denom = 1) {
      double frac = num / (denom == 0 ? 1 : denom);
      Check(frac >= 0 && frac <= 1, frac);
      return frac switch {
        (< 0.9995) => $"{frac:F3}",
        (< 9.995) => $"{frac:F2}",
        (< 99.95) => $"{frac:F1}",
        _ => $"{frac:F0}",
      };
    }

    public static string MinSec(TimeSpan x) {
      Check(x >= TimeSpan.Zero);
      long s = (long)x.TotalSeconds;
      return s < 60 ? $"{s}s" : $"{s / 60}m{s % 60}s";
    }

    public static string LocalTime(DateTime t) => t.ToLocalTime().ToString("HH:mm");
  }
}
