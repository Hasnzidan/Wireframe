using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wireframe.Models
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }
        
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total => Price * Quantity;
        
        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = null!;
        
        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; } = null!;
    }
}
