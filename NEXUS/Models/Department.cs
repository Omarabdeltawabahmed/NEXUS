using System.ComponentModel.DataAnnotations;

namespace NEXUS.Models
{
    public class Department
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Department name is required")]
        [StringLength(100)]
        public string Name { get; set; } = "";

        public string Description { get; set; } = string.Empty;

        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
