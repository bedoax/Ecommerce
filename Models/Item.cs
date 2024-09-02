using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Models
{
    public class Item
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "The price must be greater than 0.")]
        public decimal Price { get; set; }

        [Required]
        public int ProductId { get; set; }

        public Product? Product { get; set; }// This can be nullable if the relationship is optional

        // Initialize these collections to avoid "required" validation errors
        public ICollection<BrowsingHistory>? BrowsingHistories { get; set; }
        public ICollection<CartItem>? CartItems { get; set; } 
        public ICollection<RateItem>? RateItem { get; set; }
        public ICollection<OrderItem>? OrderItems { get; set; }
    }
}
