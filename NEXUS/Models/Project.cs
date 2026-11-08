using NEXUS.Enums;
using NEXUS.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace NEXUS.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Project name is required")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; }

        [Required]
        public ProjectStatus Status { get; set; } = ProjectStatus.Active;

        [ForeignKey("Department")]
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        // ربط المشروع بفريق جاهز (اختياري)
        [ForeignKey("Team")]
        public int? TeamId { get; set; }
        public Team? Team { get; set; }

        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

        // ربط المشروع بالمستخدمين (أعضاء المشروع)
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}