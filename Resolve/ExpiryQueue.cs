namespace LaMetrum.Resolve {
  class ExpiryQueue {
    record Elem(DateTime ExpiresAt, Entity Entity);

    class ElemComparer : IComparer<Elem> {
      public int Compare(Elem x, Elem y) => y.ExpiresAt.CompareTo(x.ExpiresAt);
    }

    readonly MaxHeap<Elem> _queue = new(new ElemComparer());

    public bool TryPop(DateTime now, out Entity expired) {
      expired = null;
      while (true) {
        if (_queue.Count == 0) return false;
        Elem top = _queue.Top();
        if (top.ExpiresAt > now) return false;
        _queue.Pop();
        if (top.Entity.ExpiresAt <= now) {
          expired = top.Entity;
          return true;
        }
      }
    }

    public void Push(Entity x) {
      Check(x.ExpiresAt.HasValue);
      _queue.Push(new Elem(x.ExpiresAt.Value, x));
    }

    public void Clear() {
      _queue.Clear();
    }
  }
}
