namespace Crypto.Earn.App.Frontend.Models.Communication.Frontend; 

public class LogoutRequest : IActionRequest {
    public override string ActionName => "logout";
    
    public override void Handle(MainWindow window) {
        window.AuthService.Logout();
    }
}