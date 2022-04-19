using System.Diagnostics;

namespace Crypto.Earn.App.Frontend.Models.Communication.Frontend; 

public class RedirectRequest: IActionRequest {
    public override string ActionName => "redirect";

    public string Method { get; set; }
    public string Url { get; set; }
    
    public override void Handle(MainWindow window) {
        if (string.IsNullOrWhiteSpace(Url)) return;
        
        switch (Method) {
            case "external":
                Process.Start(new ProcessStartInfo {
                    FileName = Url,
                    UseShellExecute = true
                });
                break;
        }
    }
}