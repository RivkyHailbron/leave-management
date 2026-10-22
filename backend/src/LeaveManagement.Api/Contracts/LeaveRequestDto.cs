using LeaveManagement.Api.Models;

namespace LeaveManagement.Api.Contracts;

public record LeaveRequestDto(
    int Id,
    int EmployeeId,
    string EmployeeName,
    LeaveType Type,
    DateTime StartDate,
    DateTime EndDate,
    int Days,
    LeaveStatus Status)
{
    public static LeaveRequestDto From(LeaveRequest request) => new(
        request.Id,
        request.EmployeeId,
        request.Employee?.Name ?? string.Empty,
        request.Type,
        request.StartDate,
        request.EndDate,
        request.Days,
        request.Status);
}
