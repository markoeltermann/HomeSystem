using System.Net.Http.Json;

namespace TestApp;
public class DataAnalyzer
{
    public static async Task Run()
    {
        using var httpClient = new HttpClient();

        var valueContainer1 = await httpClient.GetFromJsonAsync<ValueContainerDto>("http://sinilille:5002/api/devicePoints/138/values?from=2025-01-20&upTo=2025-01-21");
        var valueContainer2 = await httpClient.GetFromJsonAsync<ValueContainerDto>("http://sinilille:5002/api/devicePoints/139/values?from=2025-01-20&upTo=2025-01-21");

        if (valueContainer1 == null || valueContainer2 == null)
            return;

        var integral = Integrate(valueContainer1) + Integrate(valueContainer2);

        Console.WriteLine(integral);
    }

    private static double Integrate(ValueContainerDto valueContainer)
    {
        var integral = 0.0;
        //foreach (var value in valueContainer.Values)
        //{
        //    integral += (value.Value ?? 0.0) / 6;
        //}
        for (int i = 0; i < valueContainer.Values.Length - 1; i++)
        {
            integral += ((valueContainer.Values[i].Value ?? 0.0) + (valueContainer.Values[i + 1].Value ?? 0.0)) / 12;
        }
        return integral;
    }

    public class ValueContainerDto
    {
        public NumericValueDto[] Values { get; set; } = null!;
        public string Unit { get; set; } = null!;
    }

    public class NumericValueDto
    {
        public DateTime Timestamp { get; set; }
        public double? Value { get; set; }
    }
}
