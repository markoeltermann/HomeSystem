using System.Net.Http.Json;

namespace TestApp;
public class ConsumptionAnalyser
{
    private readonly HttpClient httpClient;

    public ConsumptionAnalyser()
    {
        httpClient = new HttpClient();
    }

    public static Task Run()
    {
        var ca = new ConsumptionAnalyser();
        return ca.RunInternal();
    }

    private async Task RunInternal()
    {
        var chargeValues = GetHourlyDeltas((await GetValues(87)).Values);
        var dischargeValues = GetHourlyDeltas((await GetValues(88)).Values);
        var gridBuyValues = GetHourlyDeltas((await GetValues(91)).Values);
        var loadValues = GetHourlyDeltas((await GetValues(94)).Values);
        var solarValues = GetHourlyDeltas((await GetValues(96)).Values);
        var batteryLevelValues = (await GetValues(101)).Values;
        var batteryLevelDeltas = GetHourlyDeltas(batteryLevelValues);
        var npsPriceValues = GetHourlyValues((await GetValues(144)).Values);

        var chargePowerValues = GetHourlyConsumptionFromPower((await GetValues(102)).Values);
        var gridBuyPowerValues = GetHourlyConsumptionFromPower((await GetValues(116)).Values);
        var loadPowerValues = GetHourlyConsumptionFromPower((await GetValues(136)).Values);
        var solarPowerValues = GetHourlyConsumptionFromPower((await GetValues(145)).Values);

        var count = chargeValues.Length;
        var costs = new Dictionary<DateTime, double>();

        //for (int i = 0; i < count; i++)
        //{
        //    Console.WriteLine($"{chargeValues[i].Timestamp:yyyy-MM-dd HH} {dischargeValues[i].Value - chargeValues[i].Value:0.000} {chargePowerValues[i].Value:0.000}");
        //}


        Console.WriteLine("Costs by consumption values");
        for (int i = 0; i < count; i++)
        {
            var date = chargeValues[i].Timestamp.Date;
            costs[date] = costs.GetValueOrDefault(date) + ((gridBuyValues[i].Value ?? 0) + 0.03) * (npsPriceValues[i].Value ?? 0) * 1.22;
        }

        foreach (var (date, cost) in costs)
        {
            Console.WriteLine($"{date:yyyy-MM-dd}: {cost:0.000}");
        }

        //Console.WriteLine();
        //Console.WriteLine("Costs by consumption values");
        //costs.Clear();
        //for (int i = 0; i < count; i++)
        //{
        //    var date = chargeValues[i].Timestamp.Date;
        //    costs[date] = costs.GetValueOrDefault(date) + (gridBuyPowerValues[i].Value + 0.03) * (npsPriceValues[i].Value ?? 0) * 1.22;
        //}

        //foreach (var (date, cost) in costs)
        //{
        //    Console.WriteLine($"{date:yyyy-MM-dd}: {cost:0.000}");
        //}

        //Console.WriteLine();
        //Console.WriteLine("Charge vs consumption from energy values");
        ////var currentDate = chargeValues[0].Timestamp.Date;
        //for (int i = 0; i < count; i++)
        //{
        //    var timestamp = chargeValues[i].Timestamp;
        //    var charge = (chargeValues[i].Value ?? 0) - (dischargeValues[i].Value ?? 0);

        //    var calculatedCharge = (gridBuyValues[i].Value ?? 0) - (loadValues[i].Value ?? 0) - 0.03 + (solarValues[i].Value ?? 0) * 0.8;

        //    Console.WriteLine($"{timestamp:yyyy-MM-dd HH}: {(charge >= 0 ? " " : null)}{charge:0.00} {(calculatedCharge >= 0 ? " " : null)}{calculatedCharge:0.00} {(charge - calculatedCharge):0.00} {charge / calculatedCharge:0.000}");
        //}

        //Console.WriteLine();
        //Console.WriteLine("Charge vs consumption from power values");
        ////var currentDate = chargeValues[0].Timestamp.Date;
        //for (int i = 0; i < count; i++)
        //{
        //    var timestamp = chargeValues[i].Timestamp;
        //    var charge = -(chargePowerValues[i].Value ?? 0);

        //    var calculatedCharge = ((gridBuyPowerValues[i].Value ?? 0) - (loadPowerValues[i].Value ?? 0) - 0.14 + Math.Max((solarPowerValues[i].Value ?? 0) - 0.1, 0) * 0.7) * 0.92;

        //    Console.WriteLine($"{timestamp:yyyy-MM-dd HH}: {(charge >= 0 ? " " : null)}{charge:0.000} {(calculatedCharge >= 0 ? " " : null)}{calculatedCharge:0.000} {(charge - calculatedCharge): 0.000;-0.000} {charge / calculatedCharge:0.000}");
        //}

        //Console.WriteLine();
        //Console.WriteLine("Battery level vs charge from power values");
        //for (int i = 0; i < count; i++)
        //{
        //    var timestamp = chargeValues[i].Timestamp;
        //    var charge = -chargePowerValues[i].Value / 10.3 * 100;
        //    var batteryLevelChange = batteryLevelValues[i].Value ?? 0;

        //    Console.WriteLine($"{timestamp:yyyy-MM-dd HH}: {batteryLevelChange,6: 0.0;-0.0} {charge,6: 0.0;-0.0}");
        //}

        costs.Clear();

        //Console.WriteLine();
        //Console.WriteLine("Grid buy amount vs calculated");
        ////var currentDate = chargeValues[0].Timestamp.Date;
        //for (int i = 0; i < count; i++)
        //{
        //    var timestamp = chargeValues[i].Timestamp;
        //    var date = timestamp.Date;

        //    var batteryLevelChange = batteryLevelValues[i].Value ?? 0;
        //    var charge = batteryLevelChange / 100.0 * 10.3;

        //    //var charge = -chargePowerValues[i].Value;

        //    //var calculatedCharge = (gridBuyPowerValues[i].Value - loadPowerValues[i].Value - 0.14 + Math.Max(solarPowerValues[i].Value - 0.1, 0) * 0.7) * 0.92;

        //    var calculatedGridBuyAmount = GetCalculatedGridBuyAmount(loadPowerValues, solarPowerValues, i, charge);

        //    Console.WriteLine($"{timestamp:yyyy-MM-dd HH}: {gridBuyPowerValues[i].Value,6: 0.00;-0.00} {calculatedGridBuyAmount,6: 0.00;-0.00} {gridBuyPowerValues[i].Value - calculatedGridBuyAmount,6: 0.00;-0.00}");

        //    costs[date] = costs.GetValueOrDefault(date) + (calculatedGridBuyAmount + 0.03) * (npsPriceValues[i].Value ?? 0) * 1.22;
        //}

        //foreach (var (date, cost) in costs)
        //{
        //    Console.WriteLine($"{date:yyyy-MM-dd}: {cost:0.000}");
        //}

        var r = new Random();

        var charge0 = batteryLevelValues[0].Value!.Value;
        var currentCharge = charge0;
        var charges = new (double, double)[24];
        for (int i = 0; i < 24; i++)
        {
            var delta = r.NextDouble() * (22 + 16) - 16;
            var nextCharge = currentCharge + delta;

            ClampDeltaAndNextCharge(ref delta, ref nextCharge);

            currentCharge = nextCharge;

            charges[i] = (delta, currentCharge);
        }

        var cost0 = GetCost(chargeValues, npsPriceValues, loadPowerValues, solarPowerValues, charges, 24);
        Console.WriteLine($"Cost0: {cost0:0.000}");
        //(double, double)[]? bestCharges = null;

        for (int i = 0; i < 10000; i++)
        {
            var randomisedCharges = Randomise(charge0, charges, r);
            var randomisedCost = GetCost(chargeValues, npsPriceValues, loadPowerValues, solarPowerValues, randomisedCharges, 24);

            if (randomisedCost < cost0)
            {
                cost0 = randomisedCost;
                charges = randomisedCharges;

                Console.WriteLine($"Cost{i}: {cost0:0.000}");
            }
        }

        for (int i = 0; i < 24; i++)
        {
            var (delta, charge) = charges[i];
            Console.WriteLine($"{i}: {delta: 0.00;-0.00} {charge:0.00}");
        }
    }

    private static void ClampDeltaAndNextCharge(ref double delta, ref double nextCharge)
    {
        if (nextCharge > 96)
        {
            delta -= nextCharge - 96;
            nextCharge = 96;
        }
        else if (nextCharge < 25)
        {
            delta += 25 - nextCharge;
            nextCharge = 25;
        }
    }

    private static (double, double)[] Randomise(double charge0, (double, double)[] charges, Random r)
    {
        var newCharges = new (double, double)[24];
        var currentCharge = charge0;

        for (int i = 0; i < 24; i++)
        {
            var wobble = r.NextDouble() - 0.5;
            var oldCharge = charges[i];
            var delta = oldCharge.Item1 + wobble;
            if (delta > 22)
            {
                delta = 22;
            }
            else if (delta < -16)
            {
                delta = -16;
            }
            var nextCharge = currentCharge + delta;
            ClampDeltaAndNextCharge(ref delta, ref nextCharge);
            currentCharge = nextCharge;
            newCharges[i] = (delta, currentCharge);
        }

        return newCharges;
    }

    private static double GetCost(NumericValueDto[] chargeValues, NumericValueDto[] npsPriceValues, NumericValue[] loadPowerValues, NumericValue[] solarPowerValues, (double, double)[] charges, int hourDelta)
    {
        var cost = 0.0;
        int j = 0;
        for (int i = 0; i < 24; i++)
        {
            var timestamp = chargeValues[i + hourDelta].Timestamp;
            var date = timestamp.Date;

            //var batteryLevelChange = batteryLevelDeltas[i].Value ?? 0;            
            var batteryLevelChange = charges[j++].Item1;
            var charge = batteryLevelChange / 100.0 * 10.3;

            //var charge = -chargePowerValues[i].Value;

            //var calculatedCharge = (gridBuyPowerValues[i].Value - loadPowerValues[i].Value - 0.14 + Math.Max(solarPowerValues[i].Value - 0.1, 0) * 0.7) * 0.92;

            var calculatedGridBuyAmount = GetCalculatedGridBuyAmount(loadPowerValues, solarPowerValues, i + hourDelta, charge);

            if (calculatedGridBuyAmount < 0)
            {
                calculatedGridBuyAmount = 0;
            }

            //Console.WriteLine($"{timestamp:yyyy-MM-dd HH}: {gridBuyPowerValues[i].Value,6: 0.00;-0.00} {calculatedGridBuyAmount,6: 0.00;-0.00} {gridBuyPowerValues[i].Value - calculatedGridBuyAmount,6: 0.00;-0.00}");

            cost += (calculatedGridBuyAmount + 0.03) * (npsPriceValues[i + hourDelta].Value ?? 0) * 1.22;
        }

        return cost;
    }

    private static double GetCalculatedGridBuyAmount(NumericValue[] loadPowerValues, NumericValue[] solarPowerValues, int i, double charge)
    {
        return charge / 0.95 + loadPowerValues[i].Value + 0.16 - Math.Max(solarPowerValues[i].Value - 0.1, 0) * 0.85;
    }

    private static NumericValueDto[] GetHourlyDeltas(NumericValueDto[] values)
    {
        var result = new List<NumericValueDto>();
        for (int i = 0; i < values.Length - 6; i += 6)
        {
            var v1 = values[i];
            var v2 = values[i + 6];
            var timestamp = v1.Timestamp;
            result.Add(new NumericValueDto { Timestamp = timestamp, Value = v2.Value - v1.Value });
        }
        return [.. result];
    }

    private static NumericValueDto[] GetHourlyValues(NumericValueDto[] values)
    {
        var result = new NumericValueDto[values.Length / 6];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = values[i * 6];
        }
        return result;
    }

    private static NumericValue[] GetHourlyConsumptionFromPower(NumericValueDto[] values)
    {
        var result = new NumericValue[values.Length / 6];
        for (int i = 0; i < result.Length; i++)
        {
            var numericValue = new NumericValue
            {
                Timestamp = values[i * 6].Timestamp,
                Value = ((values[i * 6].Value ?? 0)
                    + (values[i * 6 + 1].Value ?? 0)
                    + (values[i * 6 + 2].Value ?? 0)
                    + (values[i * 6 + 3].Value ?? 0)
                    + (values[i * 6 + 4].Value ?? 0)
                    + (values[i * 6 + 5].Value ?? 0)) / 6000
            };
            result[i] = numericValue;
        }
        return result;
    }

    private async Task<ValueContainerDto> GetValues(int pointId)
    {
        var result = await httpClient.GetFromJsonAsync<ValueContainerDto>($"http://sinilille:5002/api/devicePoints/{pointId}/values?from=2025-01-24&upTo=2025-01-26");
        return result ?? throw new Exception();
    }

    private class NumericValue
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }
}
