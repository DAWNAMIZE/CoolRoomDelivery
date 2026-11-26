using System.Collections.Generic;

namespace ColdRoom.Core.Models
{
    public sealed class Order
    {
        public string Id { get; set; }
        public string PublicCode { get; set; }
        public string CustomerId { get; set; }

        /// <summary>Display snapshot so staff screens do not need a user directory call.</summary>
        public string CustomerName { get; set; }

        public string StationId { get; set; }
        public OrderUrgency Urgency { get; set; }
        public OrderStatus Status { get; set; }
        public List<OrderLine> Lines { get; set; } = new();
        public long TotalKobo { get; set; }
        public string RiderId { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
    }
}
