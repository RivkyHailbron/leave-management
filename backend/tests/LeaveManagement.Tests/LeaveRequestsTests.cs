using LeaveManagement.Api.Controllers;
using LeaveManagement.Api.Data;
using LeaveManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LeaveManagement.Tests;

public class LeaveRequestsTests : IClassFixture<TestDatabase>
{
    private readonly TestDatabase _database;

    public LeaveRequestsTests(TestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public void Create_WithinQuota_Succeeds()
    {
        // Arrange
        using var db = _database.NewDb();
        var emp = new Employee { Name = "Test Emp", AnnualQuota = 20 };
        db.Employees.Add(emp);
        db.SaveChanges();

        var controller = new LeaveRequestsController(db);

        // Act: request 3 days, well within the quota.
        var result = controller.Create(new CreateLeaveRequestDto
        {
            EmployeeId = emp.Id,
            Type = LeaveType.Vacation,
            StartDate = new DateTime(2026, 3, 1),
            EndDate = new DateTime(2026, 3, 3)
        });

        // Assert
        Assert.IsType<OkObjectResult>(result);
        Assert.Single(db.LeaveRequests);
    }

    [Fact]
    public void Create_ExceedingRemainingBalance_IsRejected()
    {
        using var db = _database.NewDb();
        var emp = AddEmployee(db, quota: 20);
        AddRequest(db, emp, LeaveStatus.Approved, new DateTime(2026, 1, 6), new DateTime(2026, 1, 23));
        var controller = new LeaveRequestsController(db);

        var result = controller.Create(Vacation(emp, new DateTime(2026, 8, 2), new DateTime(2026, 8, 6)));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Single(db.LeaveRequests);
    }

    [Fact]
    public void Create_ExactlyRemainingBalance_Succeeds()
    {
        using var db = _database.NewDb();
        var emp = AddEmployee(db, quota: 20);
        AddRequest(db, emp, LeaveStatus.Approved, new DateTime(2026, 1, 6), new DateTime(2026, 1, 23));
        var controller = new LeaveRequestsController(db);

        var result = controller.Create(Vacation(emp, new DateTime(2026, 8, 2), new DateTime(2026, 8, 3)));

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void Create_OnlyApprovedRequestsOfTheSameYearCount()
    {
        using var db = _database.NewDb();
        var emp = AddEmployee(db, quota: 10);
        AddRequest(db, emp, LeaveStatus.Pending, new DateTime(2026, 3, 1), new DateTime(2026, 3, 8));
        AddRequest(db, emp, LeaveStatus.Rejected, new DateTime(2026, 4, 1), new DateTime(2026, 4, 8));
        AddRequest(db, emp, LeaveStatus.Approved, new DateTime(2025, 3, 1), new DateTime(2025, 3, 8));
        var controller = new LeaveRequestsController(db);

        var result = controller.Create(Vacation(emp, new DateTime(2026, 8, 2), new DateTime(2026, 8, 11)));

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void Create_EndDateBeforeStartDate_IsRejected()
    {
        using var db = _database.NewDb();
        var emp = AddEmployee(db, quota: 20);
        var controller = new LeaveRequestsController(db);

        var result = controller.Create(Vacation(emp, new DateTime(2026, 8, 6), new DateTime(2026, 8, 2)));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.LeaveRequests);
    }

    private static Employee AddEmployee(LeaveDbContext db, int quota)
    {
        var emp = new Employee { Name = "Test Emp", AnnualQuota = quota };
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
