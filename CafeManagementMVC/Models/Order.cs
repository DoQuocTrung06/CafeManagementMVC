using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeManagementMVC.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Chờ xác nhận";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? PaidAt { get; set; }

        public int TableId { get; set; }
        [ForeignKey("TableId")]
        public CafeTable CafeTable { get; set; }

        
        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; }
    }
}