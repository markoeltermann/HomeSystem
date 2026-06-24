using Microsoft.Extensions.Hosting;
using System.Text;
using System.Text.Json;

namespace TestApp;

public class ShellyHeatDistributionTester : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var httpClient = new HttpClient();

        var payload = new HeatDistributionMode
        {
            Mode = "fixed",
            Setpoint = 21.5,
            FixedValue = 0.0
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var jsonPayload = JsonSerializer.Serialize(payload, options);
        // call http://192.168.1.130:6705/script/1/mode

        //var content = new StringContent("{\"mode\":\"setpoint\",\"setpoint\":21.5,\"fixedValue\":0}", Encoding.UTF8, "application/json");
        //var content = new StringContent("{\"mode\":\"fixed\",\"setpoint\":21.5,\"fixedValue\":0}", Encoding.UTF8, "application/json");
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("http://192.168.1.130:6705/script/1/mode", content);

        Console.WriteLine(response.StatusCode);
        var responseString = await response.Content.ReadAsStringAsync();
        Console.WriteLine(responseString);
    }
}

public class HeatDistributionMode
{
    public string? Mode { get; set; }
    public double Setpoint { get; set; }
    public double FixedValue { get; set; }
}