using CommonLibrary.Extensions;
using CsvHelper;
using Domain;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace TestApp;
public class ModbusRegisterProcessor
{
    public static async Task Run(HomeSystemContext dbContext)
    {
        List<ushort> enabledRegisters = [];
        if (File.Exists("enabledRegisters.json"))
        {
            enabledRegisters = JsonSerializer.Deserialize<List<ushort>>(File.ReadAllText("enabledRegisters.json"))!;
        }

        using var reader = new StreamReader(@"E:\Data\Documents\Deye\deye read registers.csv");
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<ModbusRegisterModel>().ToArray();

        var processedRegisters = new List<ProcessedRegister>();

        foreach (var r in records)
        {
            if (r.Address != null)
            {
                var description = new string(r.Decription?.Where(char.IsAscii).ToArray())
                    .Replace("  ", " ").Replace("  ", " ").Replace("  ", " ").Replace("  ", " ")
                    .Replace("_", " ")
                    .Replace("\n", " ")
                    .Trim('$')
                    .Trim('-')
                    .Trim('$')
                    .Trim()
                    .Replace(" Dc ", " DC ")
                    .Capitalise();

                if (!description.Contains("undefine", StringComparison.OrdinalIgnoreCase)
                    && !description.Contains("debug", StringComparison.OrdinalIgnoreCase)
                    && (r.Note == null || !r.Note.Contains("debug", StringComparison.OrdinalIgnoreCase)))
                {
                    var unit = r.Unit?.Replace("℃", "°C").Replace("kwh", "kWh");

                    var multiplier = 1.0;

                    if (!unit.IsNullOrEmpty())
                    {
                        var multiplierString = new string(unit.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
                        if (multiplierString.Length > 0)
                        {
                            multiplier = double.Parse(multiplierString);

                            unit = new string(unit.SkipWhile(c => char.IsDigit(c) || c == '.').ToArray());
                        }
                    }
                    else if (r.Address == 609)
                    {
                        multiplier = 0.01;
                        unit = "Hz";
                    }

                    //Console.WriteLine($"{r.Address,4} {description,-50} {multiplier,6} {unit,5}");

                    if (!description.IsNullOrEmpty())
                    {
                        processedRegisters.Add(new ProcessedRegister
                        {
                            Address = (ushort)r.Address.Value,
                            Description = description,
                            Multiplier = multiplier,
                            Unit = unit,
                        });
                    }
                }
            }
        }

        var allAddresses = processedRegisters.Select(x => x.Address).ToArray();

        using var httpClient = new HttpClient();
        //httpClient.BaseAddress = new Uri("http://sinilille:5101");

        //var valueList = await httpClient.GetStringAsync("http://sinilille:5101/values?a=" + string.Join("&a=", allAddresses[..^1]));
        //var valueList = await httpClient.GetStringAsync("http://sinilille:5101/values?a=588");


        var valueList = await httpClient.GetFromJsonAsync<PointValueDto[]>("http://sinilille:5101/values?a=" + string.Join("&a=", allAddresses[..^1]));

        if (valueList == null)
        {
            Console.WriteLine("Could not get values");
            return;
        }

        var valueDict = valueList.ToDictionary(x => x.Address, x => x.Value);

        foreach (var register in processedRegisters)
        {
            var value = valueDict.GetValueOrDefault(register.Address);

            if (enabledRegisters.Contains(register.Address))
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else
            {
                Console.ResetColor();
            }

            Console.WriteLine($"{register.Address,4} {register.Description,-50} {register.Multiplier,6} {value * register.Multiplier,9:0.00} {register.Unit}");
        }

        //while (true)
        //{
        //    var addressString = Console.ReadLine();
        //    if (addressString.IsNullOrEmpty())
        //    {
        //        break;
        //    }

        //    if (ushort.TryParse(addressString, out var address))
        //    {
        //        if (!enabledRegisters.Contains(address))
        //        {
        //            enabledRegisters.Add(address);

        //            File.WriteAllText("enabledRegisters.json", JsonSerializer.Serialize(enabledRegisters));
        //        }
        //    }
        //}


        var units = dbContext.Units.ToArray();
        var dataTypes = dbContext.DataTypes.ToArray();

        var device = dbContext.Devices.Include(x => x.DevicePoints).FirstOrDefault(x => x.Name == "Deye inverter");
        if (device == null)
        {
            device = new Device { Name = "Deye inverter", IsEnabled = true, Type = "deye_inverter" };
            dbContext.Devices.Add(device);
        }

        foreach (var registerGroup in processedRegisters.Where(x => enabledRegisters.Contains(x.Address)).GroupBy(x => RemoveHighLowWord(x.Description)))
        {
            string pointAddress = null!;
            ProcessedRegister register;

            if (registerGroup.Count() == 1)
            {
                register = registerGroup.First();
                if (register.Multiplier == 1.0)
                {
                    pointAddress = $"{{{register.Address}}}";
                }
                else
                {
                    pointAddress = $"{{{register.Address}}}*{register.Multiplier}";
                }
                if (register.Unit == "°C")
                {
                    pointAddress += "-100";
                }
            }
            else if (registerGroup.Count() == 2)
            {
                var highRegister = registerGroup.Single(x => x.Description.Contains("high word", StringComparison.OrdinalIgnoreCase)
                    || x.Description.Contains("high byte", StringComparison.OrdinalIgnoreCase));
                var lowRegister = registerGroup.Single(x => x.Description.Contains("low word", StringComparison.OrdinalIgnoreCase)
                    || x.Description.Contains("low byte", StringComparison.OrdinalIgnoreCase));

                if (highRegister.Multiplier == 1.0)
                {
                    pointAddress = $"{{{highRegister.Address}}}*{(double)0x10000}";
                }
                else
                {
                    pointAddress = $"{{{highRegister.Address}}}*{highRegister.Multiplier * 0x10000}";
                }

                if (lowRegister.Multiplier == 1.0)
                {
                    pointAddress += $"+{{{lowRegister.Address}}}";
                }
                else
                {
                    pointAddress += $"+{{{lowRegister.Address}}}*{lowRegister.Multiplier}";
                }

                register = highRegister;
            }
            else
            {
                throw new InvalidOperationException();
            }

            Unit? unit = null;
            DataType dataType;
            if (register.Unit == "-")
            {
                dataType = dataTypes.First(x => x.Name == "Integer");
            }
            else
            {
                unit = units.First(x => x.Name == register.Unit);
                dataType = dataTypes.First(x => x.Name == "Float");
            }

            Console.WriteLine($"{pointAddress,-25} \"{registerGroup.Key}\"");

            if (!device.DevicePoints.Any(x => x.Address == pointAddress))
            {
                device.DevicePoints.Add(new DevicePoint
                {
                    Address = pointAddress,
                    DataType = dataType,
                    Unit = unit,
                    Name = registerGroup.Key
                });
            }
        }

        //dbContext.SaveChanges();

        //Console.WriteLine("DB updated");
    }

    private static string RemoveHighLowWord(string s)
    {
        return s.Replace("high word", "", StringComparison.OrdinalIgnoreCase)
            .Replace("low word", "", StringComparison.OrdinalIgnoreCase)
            .Replace("high byte", "", StringComparison.OrdinalIgnoreCase)
            .Replace("low byte", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private class ProcessedRegister
    {
        public ushort Address { get; set; }
        public string Description { get; set; } = null!;
        public double Multiplier { get; set; }
        public string? Unit { get; set; }
    }

    private record PointValueDto(int Address, int Value) { }
}
