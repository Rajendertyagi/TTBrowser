using System;
using System.IO;
using System.Text;
namespace TTBrowser.Services {
public static class Logger {
    private static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TTBrowser", "logs");
    private static string FilePath => Path.Combine(Dir, $"app-{DateTime.Now:yyyyMMdd}.log");
    private static readonly object _lock = new();
    public static void Init() { Directory.CreateDirectory(Dir); Info("Logger init"); }
    public static void Info(string msg) => Write("INFO", msg);
    public static void Error(string msg, Exception? ex = null) => Write("ERROR", msg + (ex != null ? " | " + ex.ToString() : ""));
    private static void Write(string lvl, string msg) { try { lock(_lock) File.AppendAllText(FilePath, $"{DateTime.Now:HH:mm:ss.fff} [{lvl}] {msg}{Environment.NewLine}", Encoding.UTF8); } catch {} }
}
}
