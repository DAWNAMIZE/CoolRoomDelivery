namespace ColdRoom.Core.Models
{
    public sealed class StorageRequest
    {
        public string Id { get; set; }
        public string CustomerId { get; set; }
        public string Description { get; set; }
        public double EstimatedKg { get; set; }
        public StorageStatus Status { get; set; }
        public string CreatedAt { get; set; }
    }
}
