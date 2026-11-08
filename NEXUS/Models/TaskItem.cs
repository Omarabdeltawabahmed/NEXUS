using NEXUS.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskStatus = NEXUS.Enums.TaskStatus;

namespace NEXUS.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Task title is required")]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public TaskStatus Status { get; set; } = TaskStatus.Pending;

        public DateTime DueDate { get; set; }

        public TaskType Type { get; set; } = TaskType.CodeSubmission;

        public string? SubmissionContent { get; set; }

        public DateTime? SubmittedAt { get; set; }

        // الحقول الجديدة الخاصة بالروابط
        public string? MeetingLink { get; set; }
        public string? ApiLink { get; set; }

        // ربط المستخدم بالـ ID واسم الكلاس
        [ForeignKey("AssignedToUser")]
        public string? AssignedToUserId { get; set; }
        public ApplicationUser? AssignedToUser { get; set; }

        [ForeignKey("Project")]
        public int ProjectId { get; set; }
        public Project? Project { get; set; }
    }
}