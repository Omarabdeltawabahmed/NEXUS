using System.ComponentModel.DataAnnotations;

namespace NEXUS.ViewModels
{
    public class CreateTaskViewModel
    {
        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public NEXUS.Enums.TaskStatus Status { get; set; }

        [Required(ErrorMessage = "Please assign the task to a user.")]
        [Display(Name = "Assign to User")]
        public string AssignedToUserId { get; set; }

        public int? ProjectId { get; set; } // لو مرتبطة بمشروع معين
    }
}