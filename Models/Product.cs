using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public int DepartmentId { get; set; }
        public Department? Department { get; set; }
        public ICollection<Item>? Items { get; set; }
       
        
    }

}
