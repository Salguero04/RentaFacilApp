using Microsoft.Maui.Storage;

namespace RentaFacil.MAUI.Services;

public class AuthService
{
    private const string AuthKey = "is_logged_in";

    public bool IsAuthenticated => Preferences.Get(AuthKey, false);

    public event Action? OnAuthStateChanged;

    public bool Login(string username, string password)
    {
        // Simple hardcoded user for now as requested
        if ((username == "admin" && password == "admin") || 
            (Preferences.Get("user_" + username, "") == password))
        {
            Preferences.Set(AuthKey, true);
            OnAuthStateChanged?.Invoke();
            return true;
        }
        return false;
    }

    public void Register(string username, string password)
    {
        Preferences.Set("user_" + username, password);
    }

    public string? GetPassword(string username)
    {
        if (username == "admin") return "admin";
        var pass = Preferences.Get("user_" + username, (string?)null);
        return string.IsNullOrEmpty(pass) ? null : pass;
    }

    public void Logout()
    {
        Preferences.Set(AuthKey, false);
        OnAuthStateChanged?.Invoke();
    }
}
