using System;

namespace DesktopTaskAid.Models
{
    public class TaskItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime? DueDate { get; set; }
        public TimeSpan? DueTime { get; set; }
        public string ReminderStatus { get; set; } // "active", "overdue", "none"
        public string ReminderLabel { get; set; }
        public string ExternalId { get; set; } // For Google Calendar sync
        public DateTime CreatedAt { get; set; }

        public TaskItem()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.Now;
            ReminderStatus = "none";
            ReminderLabel = "Not set";
        }

        public DateTime? GetFullDueDateTime()
        {
            if (DueDate == null) return null;
            if (DueTime == null) return DueDate;
            return DueDate.Value.Date.Add(DueTime.Value);
        }

        public bool IsOverdue()
        {
            var dueDateTime = GetFullDueDateTime();
            return dueDateTime.HasValue && dueDateTime.Value < DateTime.Now;
        }
    }
}
