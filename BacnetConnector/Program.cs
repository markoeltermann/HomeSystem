using BacnetConnector;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Bacnet HTTP service";
});

builder.Services.AddSingleton<BacnetService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Configure the HTTP request pipeline.

app.MapGet("/points", (BacnetService bacnetService) =>
{
    return bacnetService.ReadAllPoints();
}).WithOpenApi();

app.MapGet("/values", ([FromQuery(Name = "a")] string[] addresses, BacnetService bacnetService) =>
{
    return bacnetService.ReadValues(addresses);
}).WithOpenApi();

app.Run();
