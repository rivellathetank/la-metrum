using System.Diagnostics;

namespace LaMetrum {
  static class MonotonicClock {
    public static TimeSpan Now => TimeSpan.FromTicks(Stopwatch.GetTimestamp());
  }
}
