using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Crypto.Earn.App.Backend.Models.Config;
using Crypto.Earn.App.Backend.Services;
using Crypto.Earn.App.Frontend.Models;
using Crypto.Earn.App.Frontend.Models.Communication.Frontend;
using Crypto.Earn.App.Frontend.Services;
using Crypto.Earn.App.Frontend.Utility;
using Crypto.Earn.Common.Providers.Api;
using Crypto.Earn.Common.Services.ROO;
using Crypto.Earn.Common.Services.Versioning;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Web.WebView2.Core;
using Color = System.Drawing.Color;

namespace Crypto.Earn.App.Frontend {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window {

        private ActionRequestService actionRequestService;
        public IApi Api { get; init; }
        
        public AuthService AuthService { get; init; } 
        
        public Action<string> OnAuthenticated { get; set; }

        public MiningService MiningService;
        public LogService LogService;
        public ConfigService ConfigService { get; set; }

        public DateTime? pausedUntilDateTime;

#if DEBUG
        private DebugWindow debugWindow;
#endif
        
        public MainWindow() {
#if !DEBUG
            if (!InstallerWindow._isPersistentInstance) {
                Process.Start(InstallerWindow._persistentExecutable);
                Process.GetCurrentProcess().Kill();
                return;
            }
#endif
            
            this.actionRequestService = new ActionRequestService();
            this.Api = new ApiProvider().Create(ApiProvider.ApiVersion.V1);
            this.LogService = new LogService();
            this.ConfigService = new ConfigService();
            
            var versioningService = VersioningService.BuildFromAssembly(this.Api);
            versioningService.IsUpdateAvailable().ContinueWith(x => {
                if (x.Result)
                    versioningService.DownloadFilesAndReplace();
            });
            
            var runOnlyOnceService = new RunOnlyOnceService();
            runOnlyOnceService.OnAdditionalExecutionAttempted += ShowForm;
            runOnlyOnceService.Invoke();

            AuthService = new AuthService();
            AuthService.OnSet += AuthServiceOnOnSet;
            AuthService.OnUnset += AuthServiceOnOnUnset;

            OnAuthenticated = OnAuthenticatedCallback;
            
            InitializeComponent();
            
            this.Closing += OnClosing;
            
            this.StateChanged += OnStateChanged;
            notifyIcon.TrayMouseDoubleClick += async (sender, args) => {
                ShowForm();
            };
            notifyIcon.Visibility = Visibility.Hidden;

            AssignIcon();
            MoveWindowStartupPosition();
            ManageNoSleep();
            MonitorPause();
            MonitorBalance();
            Heartbeat();

            this.LostFocus += OnLostFocus;
            
#if DEBUG
            debugWindow = new DebugWindow(null, this);
            debugWindow.Show();
#endif
        }

        private bool minimizeIfNoFocus = true;
        private void OnLostFocus(object sender, RoutedEventArgs e) {
#if !DEBUG
            if(minimizeIfNoFocus)
                this.WindowState = WindowState.Minimized;
#endif
        }

        private async void MonitorPause() {
            while (true) {
                await Task.Delay(TimeSpan.FromSeconds(30));
//#if !DEBUG
                TogglePauseBasedOnSetDateTime();
//#endif
            }
        }

        private async void Heartbeat() {
            while (true) {
                var oauth = AuthService.GetOAuth();
                if (oauth != null) {
                    var recordedData = MiningService.GetLastRecordedEntry();
                    await Api.App.Heartbeat(oauth, recordedData.Active, recordedData.Speed, recordedData.Scale);
                }
                
                await Task.Delay(TimeSpan.FromSeconds(15));
            }
        }

        private async void MonitorBalance() {
            while (true) {
                await Task.Delay(TimeSpan.FromSeconds(15));
                var balance = await Api.App.GetBalance(AuthService.GetOAuth());
                if (balance != null) {
                    webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new {
                        action = "SyncBalance",
                        balanceMonth = balance.BalanceMonth,
                        currentBalance = balance.CurrentBalance,
                        estimatedPerMonth = balance.EstimatedPerMonth,
                        external = new {
                            miners = balance.External.Miners
                        }
                    }));
                }
            }
        }

        private async void ManageNoSleep() {
            var config = await ConfigService.Get<ApplicationConfig>();
            while (true) {
                if(config.SleepDisabled)
                    NativeMethods.SetThreadExecutionState(NativeMethods.EXECUTION_STATE.ES_CONTINUOUS | NativeMethods.EXECUTION_STATE.ES_AWAYMODE_REQUIRED);
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }
        
        private void AuthServiceOnOnSet(UserModel obj) {
            MiningService = new MiningService(obj.MinerId, LogService);
            MiningService.OnEvent += MiningServiceOnOnEvent;
            
#if DEBUG
            debugWindow.SetMiningService(MiningService);            
#endif
            
//#if !DEBUG
            MiningService.Start();
//#endif
        }

        private void AuthServiceOnOnUnset() {
            MiningService?.Stop();
            webView.Source = new Uri("https://crypto.earn/auth.html");
        }
        
        private void MiningServiceOnOnEvent(MinerEvent obj) {
            switch (obj) {
                case OnMinerStateChanged onMinerStateChanged:
                    if (onMinerStateChanged.LogEntry == null)
                        this.Dispatcher.Invoke(() => {
                            webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new {
                                action = "SyncMining",
                                speed = onMinerStateChanged.Speed ?? "0 MH/s",
                                state = onMinerStateChanged.Active ? "Active" : "Paused",
                                isActive = onMinerStateChanged.Active
                            }));
                        });
                    else
                        LogService.MinerOutput(onMinerStateChanged.LogEntry);
                    
                    break;
            }
        }

        private async void OnAuthenticatedCallback(string oauthToken) {
            webView.Source = new Uri("https://crypto.earn/home.html");
            await AuthService.SetOAuthToken(oauthToken);
            
            // Logged in, show the form.
            ShowForm();
        }

        private void AssignIcon() { // Dupe code, can't create a base class of type System.Windows.Window...
            Stream iconStream = Assembly.GetEntryAssembly()!.GetManifestResourceStream("Crypto.Earn.App.Frontend.app_icon.ico")!;
            var imageSource = new BitmapImage();
            imageSource.BeginInit();
            imageSource.StreamSource = iconStream;
            imageSource.EndInit();

            notifyIcon.Icon = new Icon(iconStream);
            this.Icon = imageSource;
        }
        private void MoveWindowStartupPosition() // Dupe code, can't create a base class of type System.Windows.Window...
        {
            const double paddingX = 10, paddingY = 10;
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - paddingX - this.Width;
            this.Top = desktopWorkingArea.Bottom - paddingY - this.Height;
        }

        private void OnClosing(object? sender, CancelEventArgs e) {
            e.Cancel = true;
            this.WindowState = WindowState.Minimized;
        }

        private void OnStateChanged(object? sender, EventArgs e) {
            if (this.WindowState == WindowState.Minimized) {
                notifyIcon.Visibility = Visibility.Visible;
                this.Hide();
            }
        }

        private void ShowForm() {
            this.Dispatcher.Invoke(async () => {
                minimizeIfNoFocus = false;
                try {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    notifyIcon.Visibility = Visibility.Hidden;

                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
                finally {
                    minimizeIfNoFocus = true;
                }
            });
        }

        private async void OnWebViewLoaded(object sender, RoutedEventArgs e) {
            await webView.EnsureCoreWebView2Async();

            var virtualPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Crypto.Earn\root");
            if (!Directory.Exists(virtualPath)) Directory.CreateDirectory(virtualPath);
            
            // TODO: check online for newer versions of HTML files.
            // TODO: if never available the use that, use embed as fallback.
            var useFallback = true;
            if (useFallback) {
                var zippedHtmlFiles = Assembly.GetEntryAssembly()!.GetManifestResourceStream("Crypto.Earn.App.Frontend.html-files.zip")!;
                zippedHtmlFiles.Seek(0, SeekOrigin.Begin);

                using var zip = new ZipArchive(zippedHtmlFiles);
                foreach (var zipArchiveEntry in zip.Entries) {
                    var fileName = virtualPath + "\\" + zipArchiveEntry.FullName.Replace("Crypto.Earn.App.Frontend.Html\\", "").Replace("Crypto.Earn.App.Frontend.Html/", "");
                    if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                        Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                    
                    if(fileName.EndsWith('\\') || fileName.EndsWith("/")) continue;

                    await using var zipFileStream = zipArchiveEntry.Open();
                    await using var externalFileStream = File.Open(fileName, FileMode.OpenOrCreate);

                    await zipFileStream.CopyToAsync(externalFileStream);
                }
            }

#if DEBUG
    
            void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target) {
                foreach (DirectoryInfo dir in source.GetDirectories())
                    CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
                foreach (FileInfo file in source.GetFiles())
                    file.CopyTo(Path.Combine(target.FullName, file.Name), true);
            }

            var dir = Environment.CurrentDirectory.Split("\\bin\\")[0] + "\\..\\Crypto.Earn.App.Frontend.Html\\";
            CopyFilesRecursively(new DirectoryInfo(dir), new DirectoryInfo(virtualPath));
            
#endif

            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "crypto.earn",
                virtualPath,
                CoreWebView2HostResourceAccessKind.DenyCors
            );
            
#if !DEBUG
            
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
            
#endif

            webView.CoreWebView2.WebMessageReceived += HandleMessage;
            
            webView.SourceChanged += WebViewOnSourceChanged;

            webView.DefaultBackgroundColor = Color.Transparent;
            webView.Source = new Uri( await AuthService.LoadOAuthToken() == null ? "https://crypto.earn/auth.html" : "https://crypto.earn/home.html");
        }

        private void WebViewOnSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e) {
            if (webView.Source.AbsolutePath == "/home.html")
                OnHomeReached();
        }

        private async Task OnHomeReached() {
            // TODO: make proper queue system.
            await Task.Delay(2500);
            var config = await ConfigService.Get<ApplicationConfig>();
            
            webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new {
                action = "SyncAccount",
                user = AuthService.GetUser(),
                settings = new {
                    autoStartup = InstallerWindow.IsInAutorun(),
                    disableSleep = config.SleepDisabled
                }
            }));
        }

        private void HandleMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e) {
            if (string.IsNullOrWhiteSpace(e?.WebMessageAsJson)) return;

            var action = JsonSerializer.Deserialize<IActionRequestHeader>(e.WebMessageAsJson)?.Action;
            if (string.IsNullOrWhiteSpace(action)) return;
            
            actionRequestService.Handle(this, action, e.WebMessageAsJson);
        }
        
        public void SetPause(DateTime pauseUntilDateTime) {
            this.pausedUntilDateTime = pauseUntilDateTime;
            TogglePauseBasedOnSetDateTime();
        }

        private void TogglePauseBasedOnSetDateTime() {
            if (MiningService == null) return;
            if (pausedUntilDateTime > DateTime.Now) 
                MiningService.Stop();
            else
                MiningService.Start();
        }
    }
}