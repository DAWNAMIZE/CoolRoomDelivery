namespace ColdRoom.Core.Models
{
    public sealed class Product
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ProductCategory Category { get; set; }
        public string Unit { get; set; }
        public long PriceKobo { get; set; }
        public int StockQty { get; set; }
        public bool IsPerishable { get; set; }
        public OrderUrgency RecommendedUrgency { get; set; }
    }
}
