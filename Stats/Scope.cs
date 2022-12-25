using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaMetrum.Stats {
  interface IScope {
    public long TotalDamage { get; }
    public long NumHits { get; }
    public TimeSpan Duration { get; }
  }
}
