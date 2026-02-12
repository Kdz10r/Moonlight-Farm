using FarmServer;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Rejestracja serwisów
builder.Services.AddSingleton<PersistenceManager>();
builder.Services.AddSingleton<SessionManager>();

var app = builder.Build();

// Graceful shutdown - zapis wszystkich sesji przy zamykaniu
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    var sessionManager = app.Services.GetRequiredService<SessionManager>();
    // Blokujemy zakończenie aplikacji do momentu zapisu
    // Uwaga: W produkcji lepiej użyć IHostedService dla bardziej złożonego zamykania
    sessionManager.SaveAllActiveSessionsAsync().GetAwaiter().GetResult();
});

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
        // Teraz GetSessionAsync może przywrócić sesję z dysku
        session = await sessionManager.GetSessionAsync(sessionId);
    }

    if (session == null)
    {
        // Jeśli ciasteczko było, ale sesja wygasła/nie istnieje na dysku -> stwórz nową
        // Jeśli nie było ciasteczka -> stwórz nową
        session = await sessionManager.CreateSessionAsync();
        
        context.Response.Cookies.Append("FARM_SESSION", session.Id, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            Expires = DateTime.UtcNow.AddDays(30) // Dłuższa ważność ciasteczka, bo sesja jest trwała na dysku
        });
    }

    context.Items["Session"] = session;

    await next();
});

app.MapGet("/", (HttpContext context) =>
{
    var session = context.Items["Session"] as Session;
    return Results.Json(new
    {
        Status = "Active",
        SessionId = session?.Id,
        LastActive = session?.LastActive,
        GameState = session?.Context.State,
        DisplayTime = session?.Context.State.Time.DisplayTime
    });
});

app.MapPost("/action/sleep", (HttpContext context) =>
{
    var session = context.Items["Session"] as Session;
    if (session == null) return Results.Unauthorized();

    TimeManager.Sleep(session.Context);
    
    return Results.Json(new
    {
        Message = "You slept until 6:00 AM the next day.",
        DisplayTime = session.Context.State.Time.DisplayTime
    });
});

app.MapPost("/action/wait", (HttpContext context, int minutes) =>
{
    var session = context.Items["Session"] as Session;
    if (session == null) return Results.Unauthorized();

    TimeManager.AdvanceTime(session.Context, minutes);

    return Results.Json(new
    {
        Message = $"You waited for {minutes} minutes.",
        DisplayTime = session.Context.State.Time.DisplayTime
    });
});

app.MapPost("/action/move", (HttpContext context, Direction direction) =>
{
    var session = context.Items["Session"] as Session;
    if (session == null) return Results.Unauthorized();

    bool moved = MovementManager.MovePlayer(session.Context, direction);

    return Results.Json(new
    {
        Success = moved,
        Position = session.Context.State.Player.Position,
        Facing = session.Context.State.Player.Facing.ToString(),
        DisplayTime = session.Context.State.Time.DisplayTime
    });
});

app.Run("http://localhost:5000");
