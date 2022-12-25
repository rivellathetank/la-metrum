using NLog;
using System.Reflection;

using E = System.Linq.Expressions.Expression;

namespace LaMetrum {
  static class Parser {
    static readonly Logger _log = LogManager.GetCurrentClassLogger();

    static readonly Dictionary<OpCode, Func<FieldReader, IMessage>> _messages = new();

    static Parser() {
      var r = E.Parameter(typeof(FieldReader), "reader");
      foreach (Type t in Assembly.GetExecutingAssembly().GetTypes()) {
        if (!t.IsClass || t.IsAbstract || !typeof(IMessage).IsAssignableFrom(t)) continue;
        if (!Enum.TryParse(t.Name, out OpCode op)) {
          throw new Exception($"missing definition for {nameof(OpCode)}.{t.Name}");
        }
        MethodInfo m = typeof(FieldReader).GetMethod(nameof(FieldReader.Read), 1, System.Type.EmptyTypes);
        Check(m.IsGenericMethodDefinition);
        m = m.MakeGenericMethod(t);
        _messages.Add(op, E.Lambda<Func<FieldReader, IMessage>>(E.Call(r, m), r).Compile());
      }
      foreach (OpCode op in Enum.GetValues<OpCode>()) {
        if (!_messages.ContainsKey(op)) {
          throw new Exception($"class {op} is missing, abstract, or does not implement IMessage");
        }
      }
    }

    public static ParsedMessage Parse(in RawMessage msg) {
      if (_messages.TryGetValue(msg.OpCode, out var ctor)) {
        FieldReader reader = new(msg.Data);
        ParsedMessage res = new(msg.OpCode, ctor.Invoke(reader));
        if (reader.RemainingBytes > 0) {
          throw new Exception($"Parser did not consume {reader.RemainingBytes} byte(s) of input data: {msg} => {res}");
        }
        try {
          res.Data.Validate();
        } catch (Exception e) {
          throw new Exception($"Message failed to validate: {res.Data}", e);
        }
        return res;
      } else {
        Check(!Enum.IsDefined(msg.OpCode));
        return new(msg.OpCode, null);
      }
    }
  }
}
