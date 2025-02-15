using Domain;
using Microsoft.EntityFrameworkCore;
using NLog.Extensions.Logging;
using SharedServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(loggingBuilder =>
{
    // configure Logging with NLog
    loggingBuilder.ClearProviders();
    loggingBuilder.SetMinimumLevel(LogLevel.Information);
    loggingBuilder.AddNLog();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Point value store HTTP service";
});

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

builder.Services.AddControllers();

builder.Services.AddDbContext<HomeSystemContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("HomeSystemContext")));

builder.Services.AddSingleton<PointValueStore>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
