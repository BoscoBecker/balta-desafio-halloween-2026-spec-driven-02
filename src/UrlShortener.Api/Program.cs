using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Endpoints;
using UrlShortener.Application;
using UrlShortener.Application.Abstractions;
using UrlShortener.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddMemoryCache(options => options.SizeLimit = 10_000);
builder.Services.AddSingleton<IShortCodeGenerator, ShortCodeGenerator>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<LinkService>();
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Default") ?? "Data Source=urlshortener.db");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.MapLinkEndpoints();
app.MapRedirectEndpoints();
app.Run();

public partial class Program;
