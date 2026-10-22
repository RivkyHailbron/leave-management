using LeaveManagement.Api.Data;
using LeaveManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Services;

public class EmployeeService
{
    private readonly LeaveDbContext _db;

    public EmployeeService(LeaveDbContext db)
    {
        _db = db;
    }

    public Task<List<Employee>> GetAllAsync(CancellationToken cancellationToken) =>
        _db.Employees
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);
}
