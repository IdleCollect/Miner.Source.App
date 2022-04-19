using System;

namespace Crypto.Earn.App.Frontend.Models.Communication.Frontend; 

public class AutoStartupSetRequest : IActionRequest {
    public override string ActionName => "auto-startup-setting";
    
    public bool Value { get; set; }
    
    public override void Handle(MainWindow window) {
        if (Value == InstallerWindow.IsInAutorun())
            return;

        try {
            InstallerWindow.RegisterAutoRun(Value);
        } catch { }
    }
}