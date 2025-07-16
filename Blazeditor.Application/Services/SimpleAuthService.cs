using System.Security.Claims;

namespace Blazeditor.Application.Services;

public class SimpleAuthService
{
    // For demo: hardcoded user
    private readonly Dictionary<string, string> _users = new()
    {
        { "admin", "password" }
    };

    public bool ValidateUser(string username, string password)
    {
        return _users.TryGetValue(username, out var pw) && pw == password;
    }

    public ClaimsPrincipal CreatePrincipal(string username)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username)
        };
        var identity = new ClaimsIdentity(claims, "Cookies");
        return new ClaimsPrincipal(identity);
    }
}
