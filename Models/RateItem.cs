namespace Ecommerce.Models
{
    public class RateItem
    {
        public int ItemId { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; }
        public User User { get; set; }
        public Item Item { get; set; }
    }
}
