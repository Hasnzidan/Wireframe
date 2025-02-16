using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wireframe.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string CustomerName { get; set; } = string.Empty;
        public int Number { get; set; }
        public int Quantity { get; set; }
        public DateOnly OrderDate { get; set; }
        public decimal Total { get; set; }
        
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
