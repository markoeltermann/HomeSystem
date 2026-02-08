using Microsoft.AspNetCore.Mvc;
using NLog.Extensions.Logging;
using SolarmanV5Connector;
using WebCommonLibrary;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Solarman V5 HTTP service";
});

builder.Services.AddSingleton<SolarmanV5Service>();
builder.Services.AddSingleton<ScheduleService>();
builder.Services.AddSingleton<InverterSettingsService>();
builder.Services.AddSingleton<TimeService>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ExceptionToProblemDetailsHandler>();

builder.Services.AddLogging(loggingBuilder =>
{
    // configure Logging with NLog
    loggingBuilder.ClearProviders();
    loggingBuilder.SetMinimumLevel(LogLevel.Information);
    loggingBuilder.AddNLog();
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseStatusCodePages();
app.UseExceptionHandler();
// Configure the HTTP request pipeline.

app.MapGet("/values", ([FromQuery(Name = "a")] int[] addresses, SolarmanV5Service solarmanService) =>
{
    return solarmanService.ReadValues(addresses);
});

app.MapPut("/values/{address}", (int address, int value, SolarmanV5Service solarmanService) =>
{
    return solarmanService.WriteValue(address, value);
});

app.MapGet("/schedule", ([FromServices] ScheduleService scheduleService) =>
{
    return scheduleService.GetSchedule();
});

app.MapPut("/schedule", (ScheduleDto schedule, [FromServices] ScheduleService scheduleService) =>
{
    return scheduleService.UpdateSchedule(schedule);
}).WithName("UpdateSchedule");

app.MapGet("/inverter-settings", ([FromServices] InverterSettingsService scheduleService) =>
{
    return scheduleService.GetSettings();
});

app.MapPut("/inverter-settings", (InverterSettingsUpdateDto settings, [FromServices] InverterSettingsService scheduleService) =>
{
    return scheduleService.UpdateSettings(settings);
});

app.MapGet("/time", ([FromServices] TimeService timeService) =>
{
    return timeService.GetTime();
});

app.MapPost("/time/sync", ([FromServices] TimeService timeService) =>
{
    var currentTime = DateTime.Now;
    var result = timeService.SetTime(currentTime);
    return result.HasValue ? Results.Ok(result.Value) : Results.InternalServerError();
});

app.Run();
