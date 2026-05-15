using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CefSharp;
using CefSharp.Wpf;
using TTBrowser.Services;

namespace TTBrowser
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<TabItem, ChromiumWebBrowser> tabs = new();
        private ChromiumWebBrowser? current;
        private readonly string home = "https://www.google.com";

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = new CefSettings();
                settings.CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TTBrowser", "CefCache");
                Directory.CreateDirectory(settings.CachePath);
                settings.LogSeverity = LogSeverity.Disable;
                Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
                Logger.Info("Cef initialized");
                StatusText.Text = "Cef initialized";
                NewTab(home);
            }
            catch (Exception ex)
            {
                Logger.Error("Cef init failed", ex);
                MessageBox.Show("CefSharp init failed:\n" + ex.Message);
            }
        }

        private void NewTab(string url)
        {
            try
            {
                var browser = new ChromiumWebBrowser(url) { Visibility = Visibility.Collapsed };
                browser.LoadingStateChanged += (s, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        StatusText.Text = e.IsLoading ? "Loading..." : "Ready";
                    });
                    Logger.Info($"LoadingStateChanged IsLoading={e.IsLoading} Url={browser.Address}");
                };
                browser.TitleChanged += (s, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        var tab = tabs.FirstOrDefault(kv => kv.Value == browser).Key;
                        if (tab != null) tab.Header = string.IsNullOrWhiteSpace(browser.Title) ? "New Tab" : browser.Title;
                    });
                };
                browser.AddressChanged += (s, e) =>
                {
                    if (browser == current) Dispatcher.Invoke(() => AddressBar.Text = e.Address);
                };
                browser.LoadError += (s, e) =>
                {
                    Logger.Error($"LoadError {e.ErrorCode} {e.ErrorText} {e.FailedUrl}");
                    Dispatcher.Invoke(() => StatusText.Text = $"Error {e.ErrorCode}: {e.ErrorText}");
                };

                ContentHost.Children.Add(browser);
                var tab = new TabItem { Header = "New Tab" };
                tabs[tab] = browser;
                TabStrip.Items.Add(tab);
                TabStrip.SelectedItem = tab;
                Logger.Info("Tab created " + url);
            }
            catch (Exception ex)
            {
                Logger.Error("NewTab failed", ex);
            }
        }

        private void TabStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabStrip.SelectedItem is TabItem tab && tabs.TryGetValue(tab, out var browser))
            {
                foreach (var b in tabs.Values) b.Visibility = Visibility.Collapsed;
                browser.Visibility = Visibility.Visible;
                current = browser;
                AddressBar.Text = browser.Address ?? "";
                browser.Focus();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e) => current?.Back();
        private void Forward_Click(object sender, RoutedEventArgs e) => current?.Forward();
        private void Reload_Click(object sender, RoutedEventArgs e) => current?.Reload();
        private void Home_Click(object sender, RoutedEventArgs e) { if (current != null) current.Load(home); }
        private void NewTab_Click(object sender, RoutedEventArgs e) => NewTab(home);
        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
        private void Logs_Click(object sender, RoutedEventArgs e)
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TTBrowser", "logs");
            Directory.CreateDirectory(dir);
            Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        }
        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && current != null)
            {
                var t = AddressBar.Text.Trim();
                if (!t.Contains(".") || t.Contains(" ")) t = "https://www.google.com/search?q=" + Uri.EscapeDataString(t);
                else if (!t.StartsWith("http")) t = "https://" + t;
                current.Load(t);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Cef.Shutdown();
        }
    }
}
