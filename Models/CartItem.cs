namespace Ecommerce.Models
{
    public class CartItem
    {
        public int CartId { get; set; }
        public int ItemId { get; set; }
        public  Cart Cart { get; set; }
        public  Item Item { get; set; }
    }

}
