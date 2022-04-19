using System;

namespace Crypto.Earn.App.Frontend.Models.Communication.Frontend; 

public class MiningPauseRequest : IActionRequest {
    public override string ActionName => "mining-pause";
    
    public int Duration { get; set; }
    
    public override void Handle(MainWindow window) {
        if (Duration <= 0) {
            window.SetPause(DateTime.MinValue);
        }
        else {
            window.SetPause(DateTime.Now.AddHours(Duration));
        }
    }
}