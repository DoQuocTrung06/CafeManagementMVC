    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace CafeManagementMVC.Models
    {
        public class User
        {
            [Key]
            public int UserId { get; set; }

            [Required]
            [StringLength(50)]
            public string Username { get; set; }

            [Required]
            public string Password { get; set; }

            [StringLength(100)]
            public string FullName { get; set; }

            public bool Status { get; set; } = true; 

            public int RoleId { get; set; }
            [ForeignKey("RoleId")]
            public Role Role { get; set; }
        }
    }