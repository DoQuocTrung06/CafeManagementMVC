using System.ComponentModel.DataAnnotations;

namespace CafeManagementMVC.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } 

        public ICollection<User> Users { get; set; }
    }
}