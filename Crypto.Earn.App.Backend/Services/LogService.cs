using System.Collections.Concurrent;
using System.Text;

namespace Crypto.Earn.App.Backend.Services; 

public class LogService {
    private readonly ConcurrentQueue<string> miningMessagePool = new ConcurrentQueue<string>();
    private readonly string miningMessagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Crypto.Earn\Logs\miner.txt");

    
    public LogService() {
        EnsureExistence(miningMessagePath, true);
        Tick();
    }

    private void EnsureExistence(string path, bool deletePrevious) {
        var directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        
        if (deletePrevious && File.Exists(path))
            File.Delete(path);
    }

    private async void Tick() {
        while (true) {
            await Task.Delay(TimeSpan.FromSeconds(15));
            await FlushMiningMessagePool();
        }
    }

    private async Task FlushMiningMessagePool() {
        try {
            var message = new StringBuilder();
            while (!miningMessagePool.IsEmpty && miningMessagePool.TryDequeue(out var line))
                message.AppendLine(line);

            if(message.Length > 0)
                await File.AppendAllTextAsync(miningMessagePath, message.ToString());
        }
        catch { /* ignored */ }
    }
    
    public void MinerOutput(string? entry) {
        if(entry != null)
            miningMessagePool.Enqueue(entry);
    }
}