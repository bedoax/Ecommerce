namespace Ecommerce.Models
{

    public class User : Person
    {
        public int Id { get; set; }
        public Address Address { get; set; }
        public Cart Cart { get; set; }
        public ICollection<RateItem> RateItems { get; set; }
        public ICollection<Order> Orders { get; set; }
        public ICollection<BrowsingHistory> BrowsingHistories { get; set; }
    }

}