using Crypto.Earn.App.Backend.Services;

namespace Crypto.Earn.App.Backend.Models.Config; 

public class ApplicationConfig : IConfig {
    public bool SleepDisabled { get; set; }

    public ApplicationConfig() { }
    public ApplicationConfig(ConfigService service) {
        this.Service = service;
    }

    public override Task Save() {
        return this.Service.Save(this);
    }
}