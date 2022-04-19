using System;

namespace Crypto.Earn.App.Frontend.Models; 

public class UserModel {
    public DateTime AuthenticationDate { get; set; }
    public string Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string MinerId { get; set; }
}