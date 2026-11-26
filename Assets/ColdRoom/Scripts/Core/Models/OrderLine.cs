namespace ColdRoom.Core.Models
{
    public sealed class OrderLine
    {
        public string ProductId { get; set; }
        public string Name { get; set; }
        public long UnitPriceKobo { get; set; }
        public int Qty { get; set; }
    }
}
