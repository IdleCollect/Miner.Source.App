using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Crypto.Earn.App.Frontend.Models.Communication.Frontend; 

public abstract class IActionRequest {
    public abstract string ActionName { get; }
    public abstract void Handle(MainWindow window);
}

public class ActionRequestService {
    private Dictionary<string, IActionRequest> mappings = new Dictionary<string, IActionRequest>();

    public ActionRequestService() {
        RegisterMappings();
    }

    private void RegisterMappings() {
        var type = typeof(IActionRequest);
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => type.IsAssignableFrom(p) && !p.IsAbstract);

        foreach (var t in types) {
            var instance = (IActionRequest)Activator.CreateInstance(t)!;
            mappings.Add(instance.ActionName, instance);
        }
    }

    public void Handle(MainWindow window, string action, string body) {
        if (mappings.TryGetValue(action, out var type)) {
            var result = (IActionRequest)JsonSerializer.Deserialize(body, type.GetType(), new JsonSerializerOptions(){PropertyNameCaseInsensitive = true})!;
            result.Handle(window);
        }
    }
}

public class IActionRequestHeader {
    [JsonPropertyName("action")]
    public string Action { get; set; }
}