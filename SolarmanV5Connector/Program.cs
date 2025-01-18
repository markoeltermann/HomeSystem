using Microsoft.AspNetCore.Mvc;
using SolarmanV5Connector;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Solarman V5 HTTP service";
});

builder.Services.AddSingleton<SolarmanV5Service>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ExceptionToProblemDetailsHandler>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseStatusCodePages();
app.UseExceptionHandler();
// Configure the HTTP request pipeline.

app.MapGet("/values", ([FromQuery(Name = "a")] int[] addresses, SolarmanV5Service bacnetService) =>
{
    return bacnetService.ReadValues(addresses);
}).WithOpenApi();

app.Run();
