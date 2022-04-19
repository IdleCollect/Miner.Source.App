using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Crypto.Earn.App.Backend.Services;
using Crypto.Earn.Common.Models.Mining;

namespace Crypto.Earn.App.Frontend; 

public partial class DebugWindow : Window {
    private MiningService miningService;
    private readonly MainWindow mainWindow;

    private MiningScaleEnum? scale;
    private DateTime? scaleChange;

    public DebugWindow(MiningService miningService, MainWindow mainWindow) {
        this.miningService = miningService;
        this.mainWindow = mainWindow;
        InitializeComponent();

        new Thread(() => {
            Tick();
        }).Start();
    }

    private void MiningServiceOnOnEvent(MinerEvent obj) {
        var parsed = obj as OnMinerStateChanged;
        if (parsed == null) return;

        this.Dispatcher.Invoke(() => {
            ActiveLabel.Content = parsed.Active.ToString();
            if(parsed.Speed != null)
                RateLabel.Content = parsed.Speed;
            if (parsed.Scale != null) {
                if (scale != parsed.Scale) {
                    scale = parsed.Scale;
                    scaleChange = DateTime.Now;

                    Intensity.Content = parsed.Scale.ToString();
                }
            }

            if (parsed.LogEntry != null) {
                Log.Children.Add(new TextBlock(){Text = parsed.LogEntry, TextWrapping = TextWrapping.Wrap});
            }
        });
    }

    private async void Tick() {
        while (true) {
            await Task.Delay(TimeSpan.FromSeconds(1));
            
            this.Dispatcher.Invoke(() => {
                PausedLabel.Content = mainWindow.pausedUntilDateTime == null || mainWindow.pausedUntilDateTime < DateTime.Now ? "False" : $"True ({mainWindow.pausedUntilDateTime.Value.Subtract(DateTime.Now):g})";
                RunningDuration.Content = scaleChange == null ? "00:00:00" : scaleChange.Value.Subtract(DateTime.Now).ToString("g");
            });
        }
    }

    public void SetMiningService(MiningService miningService) {
        this.miningService = miningService;
        miningService.OnEvent += (e) => ThreadPool.QueueUserWorkItem((_) => MiningServiceOnOnEvent(e));
    }
}