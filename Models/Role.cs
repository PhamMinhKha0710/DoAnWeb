using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWeb.Models;

public partial class Role
{
    [Key]
    public int RoleId { get; set; }

    [Required]
    [StringLength(50)]
    public string RoleName { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
