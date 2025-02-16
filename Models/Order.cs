namespace Wireframe.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public int Number { get; set; }
        public int Quantity { get; set; }
        public DateOnly OrderDate { get; set; }
        public decimal Total { get; set; }
    }
}
