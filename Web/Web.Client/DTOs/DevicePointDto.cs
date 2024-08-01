namespace Web.Client.DTOs
{
    public class DevicePointDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? DataTypeName { get; set; }
        public string? Unit { get; set; }


        public bool IsSelected { get; set; }
    }
}