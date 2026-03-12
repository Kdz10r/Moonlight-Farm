using MoonlightFarm.Server.Models;
using MoonlightFarm.Server.Services;
using MoonlightFarm.Server.Hubs;
using Microsoft.AspNetCore.HttpOverrides;

var banner = @"
  __  __                   _ _       _     _     ______                   
  |  \/  |                 | (_)     | |   | |   |  ____|                  
  | \  / | ___   ___  _ __ | |_  __ _| |__ | |_  | |__ __ _ _ __ _ __ ___  
  | |\/| |/ _ \ / _ \| '_ \| | |/ _` | '_ \| __| |  __/ _` | '__| '_ ` _ \ 
  | |  | | (_) | (_) | | | | | | (_| | | | | |_  | | | (_| | |  | | | | | | 
  |_|  |_|\___/ \___/|_| |_|_|_|\__, |_| |_|\_|  |_|  \__,_|_|  |_| |_| |_| 
                                __/ |                                     
                               |___/                                     
";

Console.WriteLine(banner);
Console.WriteLine("=================================================");
Console.WriteLine("   MOONLIGHT FARM SERVER - VERSION 3.0 (PORT 5555)");
Console.WriteLine("=================================================");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = Path.Combine(AppContext.BaseDirectory, "Client")
});

builder.WebHost.UseUrls("http://0.0.0.0:5555");

// Rejestracja serwisów
builder.Services.AddControllers();
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=moonlight_farm.db"));
builder.Services.AddSingleton<SessionManager>();
builder.Services.AddSingleton<PersistenceManager>();
builder.Services.AddSingleton<RoomManager>();
builder.Services.AddHostedService<GameLoopService>();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

app.UseDefaultFiles(); 
app.UseStaticFiles();

// Graceful shutdown
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    var sessionManager = app.Services.GetRequiredService<SessionManager>();
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
        session = await sessionManager.GetSessionAsync(sessionId);
    }

    if (session == null)
    {
        session = await sessionManager.CreateSessionAsync();
        context.Response.Cookies.Append("FARM_SESSION", session.Id, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            Expires = DateTime.UtcNow.AddDays(30)
        });
    }

    context.Items["Session"] = session;
    await next();
});

app.MapControllers();
app.MapHub<GameHub>("/gameHub");

app.Run();
