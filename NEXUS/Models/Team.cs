using System.ComponentModel.DataAnnotations;

namespace NEXUS.Models
{
    public class Team
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Team name is required")]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }


        public virtual ICollection<ApplicationUser> Users { get; set; } = new HashSet<ApplicationUser>();

        public virtual ICollection<Project> Projects { get; set; } = new HashSet<Project>();
    }
}