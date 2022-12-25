using System.Text;

namespace LaMetrum {
  enum Align {
    Left,
    Right,
  }

  record struct Row(string Id, double Weight, IReadOnlyList<string> Cells);

  interface ITable {
    public IEnumerable<Row> Print(
        IReadOnlyList<KeyValuePair<string, string>> path,
        int minWidth,
        int maxHeight);
  }

  class Table<TRow> : ITable {
    record Col(string Name, Align Align, Func<TRow, string, string> Cell);
    record RowCells(double Weight, double Order, List<string> Cells);

    readonly List<KeyValuePair<string, string>> _scalars = new();
    readonly List<Col> _cols = new();
    readonly List<RowCells> _rows = new();

    public Table(string keyCol) {
      _cols.Add(new(Name: keyCol, Align: Align.Left, Cell: (row, id) => id));
    }

    public void AddScalar(string key, string value) {
      _scalars.Add(new(key, value));
    }

    public void AddCol(string name, Align align, Func<TRow, string> cell) {
      Check(_rows.Count == 0);
      _cols.Add(new(Name: name, Align: align, Cell: (row,  id) => cell.Invoke(row)));
    }

    public void AddRow(string id, TRow row, double weight, double order) {
      _rows.Add(new RowCells(weight, order, _cols.Select(c => c.Cell.Invoke(row, id)).ToList()));
    }

    public void AddRow(string id, TRow row, double weight) => AddRow(id, row, weight, -weight);

    public IEnumerable<Row> Print(
        IReadOnlyList<KeyValuePair<string, string>> path,
        int minWidth,
        int maxHeight) {
      Check(path is not null);
      Check(_cols.Count > 0);

      int lineNum = 0;
      StringBuilder sb = new();
      List<string> line = new();

      if (lineNum++ == maxHeight) yield break;

      for (int i = 0; i != path.Count; ++i) {
        (string key, string value) = path[i];
        sb.Clear();
        sb.Append(' ');
        sb.Append(key);
        sb.Append(' ');
        if (!string.IsNullOrEmpty(value)) {
          sb.Append(value);
          sb.Append(' ');
        }
        line.Add(sb.ToString());
      }

      int gap = line.Count;
      line.Add(" ");

      foreach ((string key, string value) in _scalars) {
        sb.Clear();
        sb.Append(' ');
        sb.Append(key);
        sb.Append(' ');
        if (!string.IsNullOrEmpty(value)) {
          sb.Append(value);
          sb.Append(' ');
        }
        line.Add(sb.ToString());
      }

      List<int> colWidth = new(_cols.Count);
      for (int c = 0; c != _cols.Count; ++c) {
        colWidth.Add(_rows.Max(r => r.Cells[c].Length));
      }
      int titleWidth = line.Sum(s => s.Length);
      int totalWidth = Math.Max(titleWidth, colWidth.Sum() + _cols.Count - 1);
      if (totalWidth < minWidth) {
        colWidth[0] += minWidth - totalWidth;
        totalWidth = minWidth;
      }
      line[gap] += new string(' ', minWidth - titleWidth);
      yield return new Row(Id: null, Weight: 0, Cells: line);

      if (lineNum++ == maxHeight) yield break;
      yield return new Row(Id: null, Weight: 0, FormatRow(_cols.Select(c => c.Name).ToList()));

      foreach (RowCells row in _rows.OrderBy(x => x.Order)) {
        if (lineNum++ == maxHeight) yield break;
        yield return new Row(Id: row.Cells[0], Weight: row.Weight, FormatRow(row.Cells));
      }
      
      IReadOnlyList<string> FormatRow(IReadOnlyList<string> row) {
        int c = 0;
        line.Clear();
        foreach (string cell in row) {
          int pad = colWidth[c] - cell.Length;
          sb.Clear();
          switch (_cols[c].Align) {
            case Align.Left:
              sb.Append(cell);
              sb.Append(' ', pad);
              break;
            case Align.Right:
              sb.Append(' ', pad);
              sb.Append(cell);
              break;
          }
          if (++c != _cols.Count) sb.Append(' ');
          line.Add(sb.ToString());
        }
        return line;
      }
    }
  }
}
