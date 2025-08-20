namespace Web.Client.DTOs;
public class DevicePointDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? DataTypeName { get; set; }
    public string? Unit { get; set; }
    public int? Resolution { get; set; }

    public bool IsSelected { get; set; }
    public string Color { get; set; } = "#ffffff";
    public string SelectedValue { get; set; } = "-";
}