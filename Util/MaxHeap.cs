using System.Collections;
using System.Diagnostics;

namespace LaMetrum {
  // Binary max heap, a.k.a. priority queue.
  public class MaxHeap<T> : IReadOnlyList<T> {
    readonly List<T> _data = new();

    public MaxHeap() : this(Comparer<T>.Default) { }

    public MaxHeap(IComparer<T> cmp) {
      Check(cmp != null);
      Comparer = cmp;
    }

    public IComparer<T> Comparer { get; }

    // Returns the maximum element. O(1).
    //
    // Requires: Count > 0.
    public T Top() {
      Debug.Assert(Count > 0);
      return _data[0];
    }

    // Pushes an element. O(log Count).
    public void Push(T val) {
      _data.Add(val);
      SiftUp();
    }

    // Removes and returns the maximum element. O(log Count).
    //
    // Requires: Count > 0.
    public T Pop() {
      Debug.Assert(Count > 0);
      T res = Top();
      Drop();
      return res;
    }

    // Removes and returns the maximum element. O(log Count).
    //
    // Requires: Count > 0.
    public void Drop() {
      Debug.Assert(Count > 0);
      int last = _data.Count - 1;
      _data[0] = _data[last];
      SiftDown(0, last);
      _data.RemoveAt(last);
    }

    // Removes all elements satisfying the predicate. O(Count + N log N) where N is the number
    // of elements in the collection for which the predicate returns false. It's equal to Count
    // after RemoveAll() returns.
    //
    // The predicate is called exactly once for each element.
    public void RemoveAll(Predicate<T> match) {
      _data.RemoveAll(match);
      Heapify();
    }

    public void Clear() => _data.Clear();

    // Pushes all elements to the empty heap. O(N log N) where N is seq.Count().
    //
    // Requires: Count == 0.
    public void Assign(IEnumerable<T> seq) {
      Debug.Assert(_data.Count == 0);
      _data.AddRange(seq);
      Heapify();
    }

    public int Count => _data.Count;

    // The elements are in heap order. They are NOT sorted.
    public T this[int index] => _data[index];
    public IEnumerator<T> GetEnumerator() => _data.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void Heapify() {
      int n = _data.Count;
      if (n < 2) return;
      for (int start = (n - 2) / 2; start >= 0; --start) SiftDown(start, n);
    }

    void SiftUp() {
      if (_data.Count < 2) return;
      int last = _data.Count - 1;
      int mid = (_data.Count - 2) / 2;
      T t = _data[last];
      if (Comparer.Compare(_data[mid], t) < 0) {
        do {
          _data[last] = _data[mid];
          last = mid;
          if (mid == 0) break;
          mid = (mid - 1) / 2;
        } while (Comparer.Compare(_data[mid], t) < 0);
        _data[last] = t;
      }
    }

    void SiftDown(int start, int len) {
      int child = start;
      if (len < 2 || (len - 2) / 2 < child) return;
      child = 2 * child + 1;
      if (child + 1 < len && Comparer.Compare(_data[child], _data[child + 1]) < 0) ++child;
      if (Comparer.Compare(_data[child], _data[start]) < 0) return;
      T top = _data[start];
      do {
        _data[start] = _data[child];
        start = child;
        if ((len - 2) / 2 < child) break;
        child = 2 * child + 1;
        if (child + 1 < len && Comparer.Compare(_data[child], _data[child + 1]) < 0) {
          ++child;
        }
      } while (Comparer.Compare(_data[child], top) >= 0);
      _data[start] = top;
    }
  }
}
