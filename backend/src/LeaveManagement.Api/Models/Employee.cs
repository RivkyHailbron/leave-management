namespace LeaveManagement.Api.Models;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Annual paid-vacation quota in days.</summary>
    public int AnnualQuota { get; set; }

    public List<LeaveRequest> LeaveRequests { get; set; } = new();
}
