using System;
using System.Collections.Generic;

namespace NEXUS.ViewModels
{
    public class ReportsViewModel
    {
        // Workspace / Scope Context
        public bool IsAdmin { get; set; }
        public string WorkspaceName { get; set; } = "Global Workspace";
        public int? CurrentProjectId { get; set; }

        // Task Metrics
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int PendingTasks { get; set; }
        public double TaskCompletionRate { get; set; }

        // Priority Breakdown
        public int UrgentTasksCount { get; set; }
        public int HighTasksCount { get; set; }
        public int MediumTasksCount { get; set; }
        public int LowTasksCount { get; set; }

        // Project Metrics
        public int TotalProjects { get; set; }
        public int CompletedProjects { get; set; }
        public int ActiveProjects { get; set; }
        public double ProjectCompletionRate { get; set; }
        public int ProjectsWithDepartments { get; set; }

        // Organization & Team Metrics
        public int TotalDepartments { get; set; }
        public int TotalTeams { get; set; }
        public int TotalTeamMembers { get; set; }
        public int TotalUsers { get; set; }

        // Trend / Velocity points for dynamic SVG curve
        public List<TaskVelocityPoint> VelocityPoints { get; set; } = new();
        public string SvgCurvePath { get; set; } = string.Empty;
        public string SvgAreaPath { get; set; } = string.Empty;
        public string VelocityPeakLabel { get; set; } = "Live Rate";

        // Dynamic Project Breakdown Rows
        public List<ProjectReportItem> ProjectReports { get; set; } = new();
    }

    public class TaskVelocityPoint
    {
        public string Label { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int CompletedCount { get; set; }
        public double Rate { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class ProjectReportItem
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public double ProgressRate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
