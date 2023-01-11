using NLog;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

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
        bool? update = IsUpdateAvailable(installed);
        StringBuilder msg = new();
        msg.Append("Lost Ark version does not match LaMetrum compatibility version.\n");
        msg.Append('\n');
        msg.AppendFormat("  Lost Ark version: {0}\n", installed);
        msg.AppendFormat("  LaMetrum compatibility version: {0}\n", supported);
        msg.Append('\n');
        switch (IsUpdateAvailable(installed)) {
          case true:
            msg.Append("Please download the latest version of LaMetrum and try again.\n");
            msg.Append('\n');
            break;
          case false:
            msg.Append("Please wait for a new version of LaMetrum to be released.\n");
            msg.Append('\n');
            break;
          case null:
            break;
        }
        msg.Append("Open the LaMetrum release page in a browser?");
        DialogResult r = MessageBox.Show(
            msg.ToString(),
            Application.ProductName,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button1);
        if (r == DialogResult.Yes) {
          OpenInBrowser("https://github.com/rivellathetank/la-metrum/releases");
        }
        Environment.Exit(1);
      }
    }

    static bool? IsUpdateAvailable(Version v) {
      using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(5) };
      using HttpRequestMessage req = new(HttpMethod.Head, $"https://github.com/rivellathetank/la-metrum/tree/v{v}");
      try {
        using HttpResponseMessage resp = client.Send(req, HttpCompletionOption.ResponseHeadersRead);
        if (resp.IsSuccessStatusCode) return true;
        if (resp.StatusCode == HttpStatusCode.NotFound) return false;
        return null;
      } catch (Exception) {
        return null;
      }
    }

    static void OpenInBrowser(string url) {
      using Process p = new();
      p.StartInfo.UseShellExecute = true;
      p.StartInfo.FileName = url;
      p.Start();
    }
  }
}
