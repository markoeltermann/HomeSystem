namespace Web.Client.DTOs
{
    public class DeviceDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool? IsEnabled { get; set; }
        public IList<DevicePointDto>? Points { get; set; }

        public bool IsPointViewOpen { get; set; }
    }
}