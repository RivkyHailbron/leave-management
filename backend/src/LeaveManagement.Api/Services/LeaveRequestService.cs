using LeaveManagement.Api.Contracts;
using LeaveManagement.Api.Data;
using LeaveManagement.Api.Errors;
using LeaveManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Services;

public class LeaveRequestService
{
    private readonly LeaveDbContext _db;

    public LeaveRequestService(LeaveDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var requests = await _db.LeaveRequests
            .AsNoTracking()
            .Include(r => r.Employee)
            .OrderByDescending(r => r.StartDate)
            .ToListAsync(cancellationToken);

        return requests.Select(LeaveRequestDto.From).ToList();
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> SearchByEmployeeNameAsync(string name, CancellationToken cancellationToken)
    {
        var requests = await _db.LeaveRequests
            .AsNoTracking()
            .Include(r => r.Employee)
            .Where(r => r.Employee!.Name.Contains(name))
            .OrderByDescending(r => r.StartDate)
            .ToListAsync(cancellationToken);

        return requests.Select(LeaveRequestDto.From).ToList();
    }

    public async Task<LeaveRequestDto> CreateAsync(CreateLeaveRequestDto dto, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees.FindAsync([dto.EmployeeId], cancellationToken)
            ?? throw new NotFoundException($"Employee {dto.EmployeeId} was not found.");

        var days = (dto.EndDate - dto.StartDate).Days + 1;

        if (dto.Type == LeaveType.Vacation)
        {
            var used = await GetApprovedVacationDaysAsync(employee.Id, dto.StartDate.Year, cancellationToken);
            if (used + days > employee.AnnualQuota)
                throw new BusinessRuleException(
                    $"Not enough vacation balance: {employee.AnnualQuota - used} day(s) left, {days} requested.");
        }

        var request = new LeaveRequest
        {
            EmployeeId = employee.Id,
            Employee = employee,
            Type = dto.Type,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Days = days,
            Status = LeaveStatus.Pending
        };

        _db.LeaveRequests.Add(request);
        await _db.SaveChangesAsync(cancellationToken);
        return LeaveRequestDto.From(request);
    }

    private Task<int> GetApprovedVacationDaysAsync(int employeeId, int year, CancellationToken cancellationToken)
    {
        var yearStart = new DateTime(year, 1, 1);
        var nextYearStart = yearStart.AddYears(1);

        return _db.LeaveRequests
            .Where(r => r.EmployeeId == employeeId
                        && r.Type == LeaveType.Vacation
                        && r.Status == LeaveStatus.Approved
                        && r.StartDate >= yearStart
                        && r.StartDate < nextYearStart)
            .SumAsync(r => r.Days, cancellationToken);
    }
}
