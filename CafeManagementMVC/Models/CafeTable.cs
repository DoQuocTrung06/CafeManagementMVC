using System.ComponentModel.DataAnnotations;

namespace CafeManagementMVC.Models
{
    public class CafeTable
    {
        [Key]
        public int TableId { get; set; }

        [Required]
        [StringLength(50)]
        public string TableName { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Trống";

        public string QrCode { get; set; }

        public ICollection<Order> Orders { get; set; }
    }
}