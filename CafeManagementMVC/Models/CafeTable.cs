using System.ComponentModel.DataAnnotations;

namespace CafeManagementMVC.Models
{
    public class CafeTable
    {
        [Key]
        public int TableId { get; set; }

        [Required(ErrorMessage = "Tên bàn không được để trống")]
        [Display(Name = "Tên bàn/Số bàn")]
        public string TableName { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Trống"; 

        [Display(Name = "Mã QR")]
        public string QRCodeUrl { get; set; } 
    }
}