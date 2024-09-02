namespace Ecommerce.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int UserId { get; set; }
        public int Amount { get; set; }
        public  User? User { get; set; }
        public  TrackingOrder? TrackingOrder { get; set; }
        public  ICollection<OrderItem>? OrderItems { get; set; }
    }

}
