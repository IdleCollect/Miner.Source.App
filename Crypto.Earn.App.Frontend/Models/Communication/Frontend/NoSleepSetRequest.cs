using System;
using Crypto.Earn.App.Backend.Models.Config;

namespace Crypto.Earn.App.Frontend.Models.Communication.Frontend; 

public class NoSleepSetRequest : IActionRequest {
    public override string ActionName => "no-sleep-setting";
    
    public bool Value { get; set; }
    
    public override async void Handle(MainWindow window) {
        var config = (await window.ConfigService.Get<ApplicationConfig>());
        if (Value == config.SleepDisabled)
            return;

        try {
            config.SleepDisabled = Value;
            await config.Save();
        } catch { }
    }
}