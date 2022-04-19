using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crypto.Earn.Common.Providers.Api;

namespace Crypto.Earn.App.Frontend.Models.Communication.Frontend; 

public class AuthActionRequest : IActionRequest {
    public override string ActionName => "authenticate";

    public string Method { get; set; }
    public AuthRequestData Data { get; set; }
    
    public override void Handle(MainWindow window) {
        switch (Method) {
            case "redirect":
                Process.Start(new ProcessStartInfo {
                    FileName = Data.Url,
                    UseShellExecute = true
                });
                MonitorLogin(Data.Code, window.Api, new CancellationTokenSource(), window);
                break;
        }
    }

    private static readonly List<CancellationTokenSource> monitorCancellationTokens = new List<CancellationTokenSource>();
    private async void MonitorLogin(string code, IApi api, CancellationTokenSource cancellationToken, MainWindow window) {
        // Keep at most 3 active.
        monitorCancellationTokens.Add(cancellationToken);
        if (monitorCancellationTokens.Count > 3) {
            monitorCancellationTokens[^1].Cancel();
            monitorCancellationTokens.RemoveAt(monitorCancellationTokens.Count-1);
        }

        // Monitor if this code gets completed on the server.
        while (!cancellationToken.Token.IsCancellationRequested) {
            await Task.Delay(1000, cancellationToken.Token);
            
            var result = await api.Authentication.Complete(code);
            if (result?.Completed == true) {
                
                // Cancel all other requests.
                foreach (var monitorCancellationToken in monitorCancellationTokens) monitorCancellationToken.Cancel();
                monitorCancellationTokens.Clear();
                
                // Invoke completion event.
                window.OnAuthenticated?.Invoke(result.OAuth);
                
                break;
            }
        }
    }
}

public class AuthRequestData {
    public string Code { get; set; }
    public string Url { get; set; }
}