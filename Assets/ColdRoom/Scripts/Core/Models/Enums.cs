namespace ColdRoom.Core.Models
{
    public enum ProductCategory
    {
        Meat,
        Fish,
        Poultry,
        Vegetables,
        Other
    }

    public enum OrderUrgency
    {
        Standard,
        Priority,
        Urgent
    }

    public enum OrderStatus
    {
        Placed,
        Confirmed,
        Packed,
        AssignedToRider,
        InTransit,
        ArrivedAtPickup,
        Received
    }

    public enum StorageStatus
    {
        Requested,
        Approved,
        Stored,
        ReleaseRequested,
        Released
    }
}
