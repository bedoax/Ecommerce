namespace Ecommerce.Models
{
    public class BrowsingHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ItemId { get; set; }
        public  User User { get; set; }
        public  Item Item { get; set; }
        public DateTime Timestamp { get; set; }
    }

}
