using NEXUS.Models;

namespace NEXUS.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int ActiveProjects { get; set; }
        public int TotalDepartments { get; set; }
        public double ProductivityIndex { get; set; }

        // إحصائيات توزيع المهام للـ Chart
        public int TasksToDo { get; set; }
        public int TasksInProgress { get; set; }
        public int TasksCompletedCount { get; set; }

        public IEnumerable<TaskItem> RecentTasks { get; set; } = new List<TaskItem>();
    }
}