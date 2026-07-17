namespace SmartFollowUp.API.Enums
{
    public enum UserRole
    {
        Doctor,
        Patient,
        Admin
    }

    public enum CaseStatus
    {
        Active,
        Closed,
        Archived
    }

    public enum RiskLevel
    {
        Stable,
        Moderate,
        Critical
    }

    public enum AlertType
    {
        Critical,
        Escalation,
        Warning
    }

    public enum AlertPriority
    {
        Low,
        Medium,
        High
    }

    public enum AlertStatus
    {
        Open,
        Handled
    }

    public enum NotificationType
    {
        Reminder,
        Alert,
        Prescription,
        Instruction
    }

    public enum MedicationStatus
    {
        Taken,
        Missed,
        Pending
    }

    public enum DoctorRequestStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public enum Gender
    {
        Male,
        Female
    }
}