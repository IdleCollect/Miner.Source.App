using System;
using System.Threading;
using System.Windows;

namespace Crypto.Earn.App.Frontend.Models.Communication.Frontend; 

public class QuitRequest : IActionRequest {
    public override string ActionName => "quit";
    
    public override void Handle(MainWindow window) {
        new Thread(() => {
            if (MessageBox.Show("You are about to stop this application & the miner, this will stop generating profits from your PC.", "Closing IdleCollect", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                window.SetPause(DateTime.Now.AddHours(1));
                System.Environment.Exit(0);
            }
        }).Start();
    }
}