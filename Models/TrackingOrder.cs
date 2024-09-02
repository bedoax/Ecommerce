namespace Ecommerce.Models
{
    public class TrackingOrder
    {
        public int OrderId { get; set; }
        public int ItemId { get; set; }
        public string Status { get; set; }

        public OrderItem OrderItem { get; set; }
    }


}
