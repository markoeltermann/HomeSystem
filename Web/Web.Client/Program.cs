using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });

builder.Services.AddBlazorBootstrap();

// Register the WindowSizeService as a singleton to maintain state across components
builder.Services.AddSingleton<WindowSizeService>();

await builder.Build().RunAsync();
