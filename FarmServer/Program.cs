using FarmServer;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Rejestracja menedżera sesji jako Singleton
builder.Services.AddSingleton<SessionManager>();

var app = builder.Build();

// Konfiguracja nagłówków proxy dla Cloudflare Tunnel (aby poprawnie wykrywać HTTPS)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Middleware do obsługi sesji
app.Use(async (context, next) =>
{
    var sessionManager = context.RequestServices.GetRequiredService<SessionManager>();
    string? sessionId = context.Request.Cookies["FARM_SESSION"];
    Session? session = null;

    if (!string.IsNullOrEmpty(sessionId))
    {
        session = sessionManager.GetSession(sessionId);
    }

    if (session == null)
    {
        session = sessionManager.CreateSession();
        // Ustawienie bezpiecznego ciasteczka
        context.Response.Cookies.Append("FARM_SESSION", session.Id, new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Wymagane dla HTTPS (Cloudflare Tunnel to zapewnia)
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            Expires = DateTime.UtcNow.AddDays(7)
        });
    }

    // Przekazanie sesji do kontekstu żądania, aby endpointy miały do niej dostęp
    context.Items["Session"] = session;

    await next();
});

// Endpoint testowy zwracający stan sesji
app.MapGet("/", (HttpContext context) =>
{
    var session = context.Items["Session"] as Session;
    return Results.Json(new
    {
        Status = "Active",
        SessionId = session?.Id, // W produkcji można to ukryć, tu dla debugu
        LastActive = session?.LastActive,
        Context = "Empty Game Context"
    });
});

// Uruchomienie serwera lokalnie na porcie 5000
app.Run("http://localhost:5000");
