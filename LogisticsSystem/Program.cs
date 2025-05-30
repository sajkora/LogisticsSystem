using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using LogisticsSystem.Services;
using LogisticsSystem.Services.Contracts;
using LogisticsSystem.Hubs;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Add SignalR
builder.Services.AddSignalR();

// Rejestracja MongoDbContext i serwisów
builder.Services.AddSingleton<LogisticsSystem.MongoDbContext>();
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var context = sp.GetRequiredService<LogisticsSystem.MongoDbContext>();
    return context.Database;
});
builder.Services.AddScoped<IUserService, LogisticsSystem.Services.UserService>();
builder.Services.AddScoped<LogisticsSystem.Services.Contracts.ICourseService, LogisticsSystem.Services.CourseService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ICourseEventService, CourseEventService>();
builder.Services.AddScoped<IChatService, ChatService>();

// Konfiguracja JWT
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
        };

        // Dodajemy obsugę odczytywania tokena z ciasteczka
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token))
                {
                    // Próbujemy odczytać token z ciasteczka "AuthToken"
                    context.Token = context.Request.Cookies["AuthToken"];
                }
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // Skip the default logic.
                context.HandleResponse();
                context.Response.Redirect("/Auth/Login");
                return Task.CompletedTask;
            }
        };
    });


JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Your custom middleware (optional, but not needed for 403 anymore)
app.Use(async (context, next) =>
{
    await next();
    if (context.Response.StatusCode == 401)
    {
        context.Response.Redirect("/Auth/Login");
    }
    // No need to handle 403 here anymore
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Add SignalR hub endpoint
app.MapHub<ChatHub>("/chatHub");

app.Run();
