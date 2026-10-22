using LeaveManagement.Api.Data;
using LeaveManagement.Api.Errors;
using LeaveManagement.Api.Models;
using LeaveManagement.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LeaveManagement.Tests;

public class ApproveLeaveRequestTests : IClassFixture<TestDatabase>
{
    private readonly TestDatabase _database;

    public ApproveLeaveRequestTests(TestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task Approve_PendingRequest_SetsStatusToApproved()
    {
        using var db = _database.NewDb();
        var emp = AddEmployee(db, quota: 20);
        var request = AddRequest(db, emp, LeaveStatus.Pending, days: 3);

        var approved = await new LeaveRequestService(db).ApproveAsync(request.Id, default);

        Assert.Equal(LeaveStatus.Approved, approved.Status);
        Assert.Equal(LeaveStatus.Approved, (await Reload(db, request.Id)).Status);
    }

    [Fact]
    public async Task Approve_UnknownRequest_ThrowsNotFound()
    {
        using var db = _database.NewDb();

        await Assert.ThrowsAsync<NotFoundException>(() => new LeaveRequestService(db).ApproveAsync(999, default));
    }

    [Theory]
    [InlineData(LeaveStatus.Approved)]
    [InlineData(LeaveStatus.Rejected)]
    public async Task Approve_AlreadyProcessedRequest_ThrowsConflict(LeaveStatus status)
    {
        using var db = _database.NewDb();
        var emp = AddEmployee(db, quota: 20);
        var request = AddRequest(db, emp, status, days: 3);

        await Assert.ThrowsAsync<ConflictException>(() => new LeaveRequestService(db).ApproveAsync(request.Id, default));
    }

    [Fact]
    public async Task Approve_WhenItWouldExceedQuota_IsRejected()
    {
        using var db = _database.NewDb();
        var emp = AddEmployee(db, quota: 10);
        AddRequest(db, emp, LeaveStatus.Approved, days: 8);
        var request = AddRequest(db, emp, LeaveStatus.Pending, days: 3);

        await Assert.ThrowsAsync<BusinessRuleException>(() => new LeaveRequestService(db).ApproveAsync(request.Id, default));
        Assert.Equal(LeaveStatus.Pending, (await Reload(db, request.Id)).Status);
    }

    [Fact]
    public async Task Approve_SickLeave_IsNotLimitedByQuota()
    {
        using var db = _database.NewDb();
        var emp = AddEmployee(db, quota: 2);
        var request = AddRequest(db, emp, LeaveStatus.Pending, days: 5, type: LeaveType.Sick);

        var approved = await new LeaveRequestService(db).ApproveAsync(request.Id, default);

        Assert.Equal(LeaveStatus.Approved, approved.Status);
    }

    [PostgresOnlyFact]
    public async Task Approve_SameRequestConcurrently_SucceedsExactlyOnce()
    {
        using var db = _database.NewDb();
        var emp = AddEmployee(db, quota: 20);
        var request = AddRequest(db, emp, LeaveStatus.Pending, days: 3);

        var outcomes = await ApproveConcurrently(db, request.Id, request.Id);

        Assert.Equal(1, outcomes.Count(o => o is null));
        Assert.Single(outcomes, o => o is ConflictException);
    }

    [PostgresOnlyFact]
    public async Task Approve_TwoRequestsConcurrently_DoNotExceedQuota()
    {
        using var db = _database.NewDb();
        var emp = AddEmployee(db, quota: 10);
        AddRequest(db, emp, LeaveStatus.Approved, days: 6);
        var first = AddRequest(db, emp, LeaveStatus.Pending, days: 4);
        var second = AddRequest(db, emp, LeaveStatus.Pending, days: 4);

        var outcomes = await ApproveConcurrently(db, first.Id, second.Id);

        Assert.Equal(1, outcomes.Count(o => o is null));
        Assert.Single(outcomes, o => o is BusinessRuleException);
        var approvedDays = await db.LeaveRequests.Where(r => r.Status == LeaveStatus.Approved).SumAsync(r => r.Days);
        Assert.Equal(10, approvedDays);
    }

    // Each approval runs on its own DbContext (its own connection), like two concurrent HTTP requests.
    private static async Task<Exception?[]> ApproveConcurrently(LeaveDbContext db, int firstId, int secondId)
    {
        var connectionString = db.Database.GetConnectionString();
        using var start = new Barrier(2);

        Task<Exception?> Approve(int id) => Task.Run<Exception?>(async () =>
        {
            await using var context = new LeaveDbContext(
                new DbContextOptionsBuilder<LeaveDbContext>().UseNpgsql(connectionString).Options);
            await context.Database.OpenConnectionAsync();
            start.SignalAndWait();
            try
            {
                await new LeaveRequestService(context).ApproveAsync(id, default);
                return null;
            }
            catch (DomainException ex)
            {
                return ex;
            }
        });

        return await Task.WhenAll(Approve(firstId), Approve(secondId));
    }

    private static async Task<LeaveRequest> Reload(LeaveDbContext db, int id) =>
        await db.LeaveRequests.AsNoTracking().SingleAsync(r => r.Id == id);

    private static Employee AddEmployee(LeaveDbContext db, int quota)
    {
        var emp = new Employee { Name = "Test Emp", AnnualQuota = quota };
        db.Employees.Add(emp);
        db.SaveChanges();
        return emp;
    }

    private static LeaveRequest AddRequest(
        LeaveDbContext db, Employee emp, LeaveStatus status, int days, LeaveType type = LeaveType.Vacation)
    {
        var start = new DateTime(2026, 6, 1);
        var request = new LeaveRequest
        {
            EmployeeId = emp.Id,
            Type = type,
            StartDate = start,
            EndDate = start.AddDays(days - 1),
            Days = days,
            Status = status
        };
        db.LeaveRequests.Add(request);
        db.SaveChanges();
        return request;
    }
}
