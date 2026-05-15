using System.Windows;
using TTBrowser.Services;
namespace TTBrowser {
public partial class App : Application {
    public App() {
        Logger.Init();
        Logger.Info("App start - CefSharp");
        DispatcherUnhandledException += (s,e)=>{ Logger.Error("UI", e.Exception); MessageBox.Show(e.Exception.Message); e.Handled=true; };
    }
}
}
