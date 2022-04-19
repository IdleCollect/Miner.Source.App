using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.IO;
using System.Net.Http;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Crypto.Earn.Common.Providers.Api;

namespace Crypto.Earn.App.Frontend {
    /// <summary>
    /// Interaction logic for InstallerWindow.xaml
    /// </summary>
    public partial class InstallerWindow : System.Windows.Window {
        const string APPLICATION_NAME = "Crypto.Earn";
        const string AUTORUN_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public static bool _isPersistentInstance;
        public static string _persistentExecutable;
        private readonly string _desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private readonly string _appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Crypto.Earn");
        private readonly IApi api;
        private MainWindow mainWindow;
        
        public InstallerWindow() {
            _persistentExecutable = Path.Combine(_appData, $"{APPLICATION_NAME}.exe");
            _isPersistentInstance = _persistentExecutable == Environment.ProcessPath;
            api = new ApiProvider().Create(ApiProvider.ApiVersion.V1);
            
            RegisterGlobalExceptionReceiver();
            
            if (IsInstalled())
                mainWindow = ShowMainWindow();

            InitializeComponent();
            MoveWindowStartupPosition();
            AssignIcon();
        }

        private void RegisterGlobalExceptionReceiver() {
            AppDomain.CurrentDomain.UnhandledException  += (sender, args) => {
                api.Logs.Crash(mainWindow?.AuthService?.GetUser()?.Id, args.ExceptionObject as Exception);
            };
        }
        
        private void AssignIcon() {
            Stream iconStream = Assembly.GetEntryAssembly()!.GetManifestResourceStream("Crypto.Earn.App.Frontend.app_icon.ico")!;
            var imageSource = new BitmapImage();
            imageSource.BeginInit();
            imageSource.StreamSource = iconStream;
            imageSource.EndInit();

            this.Icon = imageSource;
        }
        private void MoveWindowStartupPosition()
        {
            const double paddingX = 10, paddingY = 10;
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - paddingX - this.Width;
            this.Top = desktopWorkingArea.Bottom - paddingY - this.Height;
        }

        private bool IsInstalled() {
            try {
                var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\idlecollect\\earn", Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (key?.GetValue("installed") != null)
                    return true;
                return false;
            }
            catch {
                return false;
            }
        }

        private void SetInstalled() {
            var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\idlecollect\\earn", Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree);
            if(key == null)
                key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\idlecollect\\earn", Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree);
            key.SetValue("installed", 1);
        }
        
        private async Task<(bool success, string? errorMessage)> Install() {
            Directory.CreateDirectory(_appData);

            var currentLocation = Environment.ProcessPath;
            if (string.IsNullOrEmpty(currentLocation)) {
                return (false, "Cannot find currently running executable. Ensure that the installer is running as admin, and that your antivirus has not blocked the installation.");
            }

            if (currentLocation != _persistentExecutable) {
                try {
                    // Preserve application in the persistent storage
                    File.Copy(currentLocation, _persistentExecutable, true);
                }
                catch {
                    return (false, "Failed to move file into a persistent location. Ensure that your antivirus is not blocking the installer, and try again.");
                }
            }

            // Install WebView2
            if (!IsWebView2Installed()) {
                var webView2Setup = Path.GetTempFileName();

                using (var client = new HttpClient()) {
                    var response = await client.GetAsync("https://go.microsoft.com/fwlink/p/?LinkId=2124703");

                    using (var stream = new FileStream(webView2Setup, FileMode.Create))
                        await response.Content.CopyToAsync(stream);
                }

                var setup = Process.Start(webView2Setup, "/silent /install");
                await setup.WaitForExitAsync();
                File.Delete(webView2Setup);
            }

            if (this.AddToStartupCheckbox.IsChecked == true && !IsInAutorun()) {
                RegisterAutoRun();
            }

            if (this.CreateDesktopShortcutCheckbox.IsChecked == true && !HasDesktopShortcut()) {
                // Create desktop shortcut
                var deskDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

                try {
                    await using StreamWriter writer = new StreamWriter(deskDir + "\\" + APPLICATION_NAME + ".url");
                    await writer.WriteLineAsync("[InternetShortcut]");
                    await writer.WriteLineAsync("URL=file:///" + _persistentExecutable);
                    await writer.WriteLineAsync("IconIndex=0");
                    var icon = _persistentExecutable.Replace('\\', '/');
                    await writer.WriteLineAsync("IconFile=" + icon);
                }
                catch {
                    // Failed to create shortcut, not a critical error, thus should be ignored.
                }
            }

            api.Logs.Analytics(10001, null, new Dictionary<string, string>() {
                { "settings:create_desktop_shortcut", this.CreateDesktopShortcutCheckbox.IsChecked?.ToString() ?? "false" },
                { "settings:add_auto_startup", this.AddToStartupCheckbox.IsChecked?.ToString() ?? "false" },
            });
            SetInstalled();
            return (true, null);
        }

        private bool IsWebView2Installed() {
            var key = @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}";

            if (Environment.Is64BitOperatingSystem)
                key = @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}";

            using (var regKey = Registry.LocalMachine.OpenSubKey(key))
                return !string.IsNullOrWhiteSpace(regKey?.GetValue("pv")?.ToString());
        }

        public static bool IsInAutorun() {
            using (var regKey = Registry.CurrentUser.OpenSubKey(AUTORUN_KEY)) {
                var value = regKey?.GetValue(APPLICATION_NAME)?.ToString();
                if (string.IsNullOrWhiteSpace(value)) return false;

                return value == _persistentExecutable;
            }
        }

        public static void RegisterAutoRun(bool enable = true) {
            try {
                if(enable)
                    using (var regKey = Registry.CurrentUser.OpenSubKey(AUTORUN_KEY, true))
                        regKey?.SetValue(APPLICATION_NAME, _persistentExecutable);
                else 
                    Registry.CurrentUser.DeleteSubKey(AUTORUN_KEY, false);
            }
            catch {
                // Failed to add to startup, not a critical error, thus should be ignored.
            }
        }

        private bool HasDesktopShortcut() {
            foreach (var file in Directory.GetFiles(_desktop, "*.url")) {
                var fileContent = File.ReadAllLines(file);
                if (fileContent.Any(x=>x == $"URL=file:///{_persistentExecutable}")) return true;
            }

            return false;
        }

        private async void InstallButton_OnClick(object sender, RoutedEventArgs e) {
            // Disable form for modifications.
            FormBorder.IsHitTestVisible = false;
            InstallButton.Content = "Installing...";
            InstallButton.Opacity = 0.5;
            CancelButton.Visibility = Visibility.Hidden;
            ErrorBox.Visibility = Visibility.Hidden;

            var installResult = await Install();
            if (!installResult.success) {
                // Failed to install, reverse the installation state and show error message.
                FormBorder.IsHitTestVisible = true;
                InstallButton.Content = "Install";
                InstallButton.Opacity = 1;
                CancelButton.Visibility = Visibility.Visible;
                ErrorBox.Visibility = Visibility.Visible;
                ErrorMessage.Text = installResult.errorMessage;
                return;
            }
            
            // Installed successfully, show next form.
            mainWindow = ShowMainWindow();
        }

        private MainWindow ShowMainWindow() {
            this.Hide();
            var mainWindow = new MainWindow();
            mainWindow.Show();
            
            return mainWindow;
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e) {
            if(MessageBox.Show("By clicking Yes you will cancel the installation, thus no modifications to your system will occur.", "Are you sure you want to cancel the installation process?", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                Environment.Exit(0);
        }
    }
}
