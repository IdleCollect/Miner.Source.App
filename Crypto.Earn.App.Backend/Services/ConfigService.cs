using System.Text.Json;
using Crypto.Earn.App.Backend.Models.Config;

namespace Crypto.Earn.App.Backend.Services; 

public class ConfigService {
    private object? cache;
    private readonly string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Crypto.Earn\settings.config");

    public async Task<T> Get<T>() {
        if(cache == null)
            return await Load<T>();
        return (T)cache;
    }

    public async Task<T> Load<T>() {
        if (!File.Exists(configPath)) 
            return await CreateNewInstance<T>();

        try {
            var text = await File.ReadAllTextAsync(configPath);
            var loaded = JsonSerializer.Deserialize<T>(text) ?? await CreateNewInstance<T>();
            ((IConfig)(object)loaded).Service = this;
            cache = loaded;
            return loaded;
        }
        catch (Exception e) {
            return await CreateNewInstance<T>();
        }
    }

    public async Task Save<T>(T config) {
        this.cache = config;
        try {
            await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions() { WriteIndented = true }));
        } catch { /* settings will not save, non-critical issue. */ }
    }

    private async Task<T> CreateNewInstance<T>() {
        var freshInstance = (T)Activator.CreateInstance(typeof(T), new object[] {this})!;
        await Save(freshInstance);
        return freshInstance;
    }
}

public abstract class IConfig {
    public ConfigService Service;

    public abstract Task Save();
}