using NLog;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace LaMetrum {
  static class Program {
    static readonly Logger _log = LogManager.GetCurrentClassLogger();

    [STAThread]
    static int Main(string[] args) {
      try {
        InitLocale();
        Check(BitConverter.IsLittleEndian);

        if (args.Length == 3 && args[0] == "dump") {
          Dump.DumpAll(srcDir: args[1], dstDir: args[2]);
          return 0;
        }

        Check(args.Length == 0);
        CheckVersion();
        RequestFirewallException();
        using GUI gui = new();
        Application.Run(gui);
        return 0;
      } catch (Exception e) {
        _log.Fatal("Unhandled exception: {0}", e.ToString());
        MessageBox.Show($"Unhandled exception: {e}", Application.ProductName, MessageBoxButtons.OK);
        return 1;
      }
    }

    static void InitLocale() {
      // AppContext.SetSwitch("System.Globalization.Invariant", true);
      Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
      Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
      CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
      CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
    }

    static void RequestFirewallException() {
      const int RandomPort = 20789;
      TcpListener x = new(Dns.GetHostEntry(Dns.GetHostName()).AddressList[0], RandomPort);
      x.Start();
      x.Stop();
    }

    static void CheckVersion() {
      Version supported = GameInfo.SupportedVersion;
      Version installed = GameInfo.InstalledVersion();
      if (installed is null) {
        MessageBox.Show(
            $"Cannot find Lost Ark. Make sure it is installed.",
            Application.ProductName, MessageBoxButtons.OK);
        Environment.Exit(1);
      }
      if (supported != installed) {
        MessageBox.Show(
            $"Lost Ark version mismatch!\n\nSupported version: {supported}\n  Installed version: {installed}",
            Application.ProductName, MessageBoxButtons.OK);
        Environment.Exit(1);
      }
    }
  }
}
