using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Crypto.Earn.App.Frontend.Models;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;

namespace Crypto.Earn.App.Frontend.Services; 

public class AuthService {
    private string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Crypto.Earn\login.session");
    private readonly IJwtDecoder decoder;
    private UserModel? userModel = null!;
    private string? oauth = null!;

    public event Action<UserModel> OnSet;
    public event Action OnUnset;
    
    public AuthService() {
        this.decoder = CreateDecoder();
    }
    
    private IJwtDecoder CreateDecoder() {
        IJsonSerializer serializer = new JsonNetSerializer();
        IDateTimeProvider provider = new UtcDateTimeProvider();
        IJwtValidator validator = new JwtValidator(serializer, provider);
        IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
        IJwtAlgorithm algorithm = new HMACSHA256Algorithm(); // symmetric
        return new JwtDecoder(serializer, validator, urlEncoder, algorithm);
    }
    
    public async Task SetOAuthToken(string token) {
        try {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));

            await File.WriteAllTextAsync(path, token);
        } catch { /* Do not crash if failed.*/ }

        SetUser(token);
    }

    public async Task<UserModel?> LoadOAuthToken() {
        try {
            if (!File.Exists(path)) {
                return null;
            }

            var token = await File.ReadAllTextAsync(path);

            SetUser(token);
            return userModel;
        }
        catch (Exception ex) {
            /* Do not crash if failed.*/
        }

        return null;
    }

    public async void Logout() {
        this.oauth = null;
        this.userModel = null;
        
        try {
            if(File.Exists(path))
                File.Delete(path);
        } catch { }

        OnUnset?.Invoke();
    }

    private void SetUser(string token) {
        oauth = token;
        userModel = decoder.DecodeToObject<UserModel>(token);
        OnSet?.Invoke(userModel);
    }

    public UserModel? GetUser() {
        return userModel;
    }
    public string? GetOAuth() {
        return oauth;
    }
}