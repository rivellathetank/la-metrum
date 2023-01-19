using Microsoft.Win32;
using System.Diagnostics;
using System.Net;

namespace LaMetrum {
  static class GameInfo {
    public static Version SupportedVersion { get; } = new("1.397.435.1993691");

    public static string ProcessName => "LOSTARK";

    public static short ServerPort => 6040;

    public static IPAddress ServerIP { get; } = IPAddress.Parse("52.50.170.121");

    public static string InstallationDir() {
      string dir = @"C:\Program Files (x86)\Steam\steamapps\common\Lost Ark\Binaries\Win64";
      if (!Directory.Exists(dir)) {
        dir = (string)Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 1599340")?.GetValue("InstallLocation");
        if (dir is null) return null;
        dir = Path.Combine(dir, "Binaries", "Win64");
        if (!Directory.Exists(dir)) return null;
      }
      return dir;
    }

    public static Version InstalledVersion() {
      string dir = InstallationDir();
      if (dir is null) return null;
      string exe = Path.Combine(dir, "LOSTARK.exe");
      if (!File.Exists(exe)) return null;
      string v = FileVersionInfo.GetVersionInfo(exe).ProductVersion;
      int sep = v.IndexOf(' ');
      return new Version(sep < 0 ? v : v[0..sep]);
    }
  }
}
