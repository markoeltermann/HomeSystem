using CsvHelper.Configuration.Attributes;

namespace TestApp;
public class ModbusRegisterModel
{
    [Name("Addr")]
    public int? Address { get; set; }

    [Name("Register meaning")]
    public string? Decription { get; set; }

    [Name("R/W")]
    public string? ReadWrite { get; set; }

    [Name("data range")]
    public string? DataRange { get; set; }

    [Name("unit")]
    public string? Unit { get; set; }

    [Name("note")]
    public string? Note { get; set; }
}
