using NLog;
using System.Net;

namespace LaMetrum {
  class RecvWindow {
    const long MaxWindow = 1 << 30;
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    static readonly Logger _log = LogManager.GetCurrentClassLogger();

    readonly MaxHeap<Segment> _pending = new(new RangeComparer());
    long _pos = -1;

    public void Clear() {
      _pos = -1;
      _pending.Clear();
    }

    public IEnumerable<ArraySegment<byte>> Push(Packet p) {
      if (p.SYN) {
        _log.Info("TCP SYN: IP {0}, SeqNum {1}", new IPAddress(p.SrcIP), p.SeqNum);
        _pos = unchecked(p.SeqNum + 1);
        _pending.Clear();
        yield return new ArraySegment<byte>();
        yield break;
      }

      int len = p.Data.Count;
      // _log.Debug("TCP DAT: IP {0,10}, SeqNum {1,10}, Len {2,5}", p.SrcIP, p.SeqNum, len);
      Check(len > 0);
      if (_pos == -1) {
        _log.Info("Missed SYN from {0}. SeqNum = {1}. Parsing of the first few packets may fail.",
                  new IPAddress(p.SrcIP), p.SeqNum);
        _pos = p.SeqNum;
      }

      if (_pending.Count == 0) _pos &= 0xFFFFFFFF;

      long from = (_pos >> 32 << 32) + p.SeqNum;
      if (Math.Abs((from += 0L << 32) - _pos) >= MaxWindow &&
          Math.Abs((from += 1L << 32) - _pos) >= MaxWindow &&
          Math.Abs((from -= 2L << 32) - _pos) >= MaxWindow) {
        _log.Warn("TCP packet outside of receive window: {0} {1}", _pos, p.SeqNum);
        yield break;
      }

      long to = from + len;
      Check(to <= long.MaxValue - (1L << 32));
      DateTime now = DateTime.UtcNow;

      if (to <= _pos) {
        _log.Info("Dropping redundant received TCP packet: [{0}, {1}) <= {2}", from, to, _pos);
        yield break;
      } else if (from <= _pos) {
        int drop = (int)(_pos - from);
        if (drop > 0) {
          _log.Info("Dropping {0} redundant received TCP packet byte(s): [{1}, {2}) + [{3}, {4})",
                    drop, from, _pos, _pos, to);
        }
        yield return p.Data.Slice(drop);
        _pos = to;
      } else {
        Check(from > _pos);
        _log.Warn("Gap in TCP sequence numbers: {0} - {1} = {2}", from, _pos, from - _pos);
        _pending.Push(new Segment(Data: p.Data.ToArray(), Pos: from, Deadline: now + Timeout));
        yield break;
      }

      while (_pending.Count > 0) {
        Segment r = _pending.Top();
        len = r.Data.Length;
        from = r.Pos;
        to = from + len;
        if (to <= _pos) {
          _log.Info("Dropping redundant stashed TCP packet: [{0}, {1}) <= {2}", from, to, _pos);
          _pending.Pop();
        } else if (from <= _pos) {
          int drop = (int)(_pos - from);
          if (drop > 0) {
            _log.Info("Dropping {0} redundant stashed TCP packet byte(s): [{1}, {2}) + [{3}, {4})",
                      drop, from, _pos, _pos, to);
          }
          yield return new ArraySegment<byte>(r.Data, drop, len - drop);
          _pos = to;
          _pending.Pop();
        } else {
          Check(from > _pos);
          if (r.Deadline > now) break;
          _log.Warn("Lost {0} byte(s) of the TCP stream: [{1}, {2})", from - _pos, _pos, from);
          yield return new ArraySegment<byte>();
          _pos = from;
        }
      }
    }

    record struct Segment(byte[] Data, long Pos, DateTime Deadline);

    class RangeComparer : IComparer<Segment> {
      public int Compare(Segment x, Segment y) => y.Pos.CompareTo(x.Pos);
    }
  }
}
