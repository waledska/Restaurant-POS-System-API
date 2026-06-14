namespace WebApisApp.DTOs.Common
{
    public class LocationDto
    {
        public Guid LocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string LocationType { get; set; } = string.Empty;
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LocationCreateDto
    {
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string LocationType { get; set; } = string.Empty; // "Branch" or "Warehouse"
        public string? Address { get; set; }
    }

    public class LocationUpdateDto : LocationCreateDto
    {
        public bool IsActive { get; set; }
    }
}
