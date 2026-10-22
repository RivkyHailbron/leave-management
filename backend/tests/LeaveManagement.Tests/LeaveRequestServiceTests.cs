using LeaveManagement.Api.Data;
using LeaveManagement.Api.Errors;
using LeaveManagement.Api.Models;
using LeaveManagement.Api.Services;
using Xunit;

namespace LeaveManagement.Tests;

public class LeaveRequestServiceTests : IClassFixture<TestDatabase>
{
    private readonly TestDatabase _database;

    public LeaveRequestServiceTests(TestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task Create_WithinQuota_Succeeds()
    {
        using var db = _database.NewDb();
        var emp = AddEmployee(db, quota: 20);
        var service = new LeaveRequestService(db);

        var created = await service.CreateAsync(Vacation(emp, new DateTime(2026, 3, 1), new DateTime(2026, 3, 3)), default);

        Assert.Equal(3, created.Days);
        Assert.Equal(LeaveStatus.Pending, created.Status);
        Assert.Single(db.LeaveRequests);
    }

    [Fact]
    public async Task Create_ForUnknownEmployee_ThrowsNotFound()
    {
        using var db = _database.NewDb();
        var service = new LeaveRequestService(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateAsync(Vacation(new Employee { Id = 999 }, new DateTime(2026, 3, 1), new DateTime(2026, 3, 3)), default));
    }

    [Fact]
    public async Task Create_ExceedingRemainingBalance_IsRejected()
    {
        using var db = _database.NewDb();
        var emp = AddEmployee(db, quota: 20);
        AddRequest(db, emp, LeaveStatus.Approved, new DateTime(2026, 1, 6), new DateTime(2026, 1, 23));
        var service = new LeaveRequestService(db);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateAsync(Vacation(emp, new DateTime(2026, 8, 2), new DateTime(2026, 8, 6)), default));
        Assert.Single(db.LeaveRequests);
    }

    [Fact]
    public async Task Create_ExactlyRemainingBalance_Succeeds()
    {
        using var db = _database.NewDb();
        var emp = AddEmployee(db, quota: 20);
        AddRequest(db, emp, LeaveStatus.Approved, new DateTime(2026, 1, 6), new DateTime(2026, 1, 23));
        var service = new LeaveRequestService(db);

        var created = await service.CreateAsync(Vacation(emp, new DateTime(2026, 8, 2), new DateTime(2026, 8, 3)), default);

        Assert.Equal(2, created.Days);
    }

    [Fact]
    public async Task Create_OnlyApprovedRequestsOfTheSameYearCount()
    {
        using var db = _database.NewDb();
        var emp = AddEmployee(db, quota: 10);
        AddRequest(db, emp, LeaveStatus.Pending, new DateTime(2026, 3, 1), new DateTime(2026, 3, 8));
        AddRequest(db, emp, LeaveStatus.Rejected, new DateTime(2026, 4, 1), new DateTime(2026, 4, 8));
        AddRequest(db, emp, LeaveStatus.Approved, new DateTime(2025, 3, 1), new DateTime(2025, 3, 8));
        var service = new LeaveRequestService(db);

        var created = await service.CreateAsync(Vacation(emp, new DateTime(2026, 8, 2), new DateTime(2026, 8, 11)), default);

        Assert.Equal(10, created.Days);
    }

    [Fact]
    public async Task Search_ByPartialName_ReturnsOnlyThatEmployeesRequests()
    {
        using var db = _database.NewDb();
        var dana = AddEmployee(db, quota: 20, name: "Dana Levi");
        var yossi = AddEmployee(db, quota: 20, name: "Yossi Cohen");
        AddRequest(db, dana, LeaveStatus.Pending, new DateTime(2026, 3, 1), new DateTime(2026, 3, 2));
        AddRequest(db, yossi, LeaveStatus.Pending, new DateTime(2026, 4, 1), new DateTime(2026, 4, 2));
        var service = new LeaveRequestService(db);

        var results = await service.SearchByEmployeeNameAsync("Dan", default);

        Assert.Equal(dana.Id, Assert.Single(results).EmployeeId);
    }

    [Fact]
    public async Task Search_WithSqlInjectionPayload_IsTreatedAsPlainText()
    {
        using var db = _database.NewDb();
        var dana = AddEmployee(db, quota: 20, name: "Dana Levi");
        AddRequest(db, dana, LeaveStatus.Pending, new DateTime(2026, 3, 1), new DateTime(2026, 3, 2));
        var service = new LeaveRequestService(db);

        var results = await service.SearchByEmployeeNameAsync("nobody%') OR 1=1 --", default);

        Assert.Empty(results);
    }

    private static Employee AddEmployee(LeaveDbContext db, int quota, string name = "Test Emp")
    {
        var emp = new Employee { Name = name, AnnualQuota = quota };
        db.Employees.Add(emp);
        db.SaveChanges();
        return emp;
    }

    private static void AddRequest(LeaveDbContext db, Employee emp, LeaveStatus status, DateTime start, DateTime end)
    {
        db.LeaveRequests.Add(new LeaveRequest
        {
            EmployeeId = emp.Id,
            Type = LeaveType.Vacation,
            StartDate = start,
            EndDate = end,
            Days = (end - start).Days + 1,
            Status = status
        });
        db.SaveChanges();
    }

    private static CreateLeaveRequestDto Vacation(Employee emp, DateTime start, DateTime end) => new()
    {
        EmployeeId = emp.Id,
        Type = LeaveType.Vacation,
        StartDate = start,
        EndDate = end
    };
}
