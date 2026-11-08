using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace NEXUS.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }

        // ربط المستخدم بمشروع معين
        public int? ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }
    }
}