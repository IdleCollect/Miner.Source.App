using System.Runtime.InteropServices;
using Crypto.Earn.Common.Models.Mining;
using H.Formatters;
using H.Pipes;
using H.Pipes.Args;

namespace Crypto.Earn.App.Backend.Services; 

public class MiningService {
    private readonly LogService logService;

    [DllImport("Crypto.Earn.App.Common.Core")]
    private static extern void CoreConfigure(IntPtr stringPtr);
    
    [DllImport("Crypto.Earn.App.Common.Core")]
    private static extern void CoreStart();
    [DllImport("Crypto.Earn.App.Common.Core")]
    private static extern void CoreStop();

    public event Action<MinerEvent> OnEvent;

    private PipeServer<MinerUptimeModel> pipeServer;
    private bool isRunning;
    private MinerData minerData = new MinerData();
    
    public MiningService(string minerId, LogService logService) {
        this.logService = logService;
        
        logService.MinerOutput("Initialising");
        CreatePipeServer();
        logService.MinerOutput("Created pipe server");
        CoreConfigure(Marshal.StringToHGlobalAnsi(minerId));
        logService.MinerOutput("Configured for miner id");
        OnEvent?.Invoke(new OnMinerConfigured());
        logService.MinerOutput("Miner configured event sent");
    }

    private void CreatePipeServer() {
        pipeServer = new PipeServer<MinerUptimeModel>("miner:uptime", new SystemTextJsonFormatter());
        pipeServer.MessageReceived += OnMessageReceived;
        pipeServer.StartAsync();
    }

    private void OnMessageReceived(object? sender, ConnectionMessageEventArgs<MinerUptimeModel?> e) {
        logService.MinerOutput("Received message from miner:");
        
        if (e.Message?.ActivityMonitor != null)
            minerData.Scale = e.Message.ActivityMonitor.GetScale();
        if (e.Message?.SpeedInformation != null)
            minerData.Speed = e.Message.SpeedInformation.Value + " " + e.Message.SpeedInformation.Unit;
        if (e.Message != null)
            minerData.Active = e.Message.IsOnline;
        OnEvent?.Invoke(new OnMinerStateChanged(){Active = e.Message?.IsOnline ?? false, Speed = (e.Message?.SpeedInformation != null ? Math.Round(e.Message.SpeedInformation.Value, 2) + " " + e.Message.SpeedInformation.Unit : "0 MH/s"), Scale = e.Message?.ActivityMonitor?.GetScale(), LogEntry = e.Message?.LogEntry });
    }

    public void Start() {
        if (isRunning) return;
        isRunning = true;
        
        logService.MinerOutput("Starting miner...");
        CoreStart();
        logService.MinerOutput("Started miner...");
    }
    public void Stop() {
        if (!isRunning) return;
        isRunning = false;
        
        logService.MinerOutput("Stopping miner...");
        CoreStop();
        logService.MinerOutput("Stopped miner...");
        OnEvent?.Invoke(new OnMinerStateChanged(){Active = false, Speed = "0 MH/s"});
    }

    public MinerData GetLastRecordedEntry() {
        return minerData;
    }
}

public class MinerEvent { }
public class OnMinerConfigured : MinerEvent { }
public class OnMinerStateChanged : MinerEvent {
    public bool Active { get; set; }
    public string? Speed { get; set; }
    public MiningScaleEnum? Scale { get; set; }
    public string? LogEntry { get; set; }
}

public class MinerData {
    public bool Active { get; set; }
    public string? Speed { get; set; }
    public MiningScaleEnum? Scale { get; set; }
}