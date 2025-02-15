using Domain;
using Microsoft.EntityFrameworkCore;
using SharedServices;
using Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddControllers();

builder.Services.AddDbContext<HomeSystemContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("HomeSystemContext")));

builder.Services.AddSingleton<PointValueStore>();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Home System UI Service";
});

builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Web.Client._Imports).Assembly);

app.MapControllers();

app.Run();
