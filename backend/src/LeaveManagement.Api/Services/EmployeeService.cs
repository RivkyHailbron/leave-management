using LeaveManagement.Api.Contracts;
using LeaveManagement.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Services;

public class EmployeeService
{
    private readonly LeaveDbContext _db;

    public EmployeeService(LeaveDbContext db)
    {
        _db = db;
    }

    public Task<List<EmployeeDto>> GetAllAsync(CancellationToken cancellationToken) =>
        _db.Employees
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => new EmployeeDto(e.Id, e.Name, e.AnnualQuota))
            .ToListAsync(cancellationToken);
}
