namespace Ecommerce.Models
{
    public class OrderItem
    {
        public int OrderId { get; set; }
        public int ItemId { get; set; }

        public Order Order { get; set; }
        public Item Item { get; set; }

        // Navigation property for TrackingOrder
        public TrackingOrder TrackingOrder { get; set; }
    }


}
