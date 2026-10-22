using System.Net;
using System.Net.Http.Json;
using LeaveManagement.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LeaveManagement.Tests;

// Runs against the app's seed data: Dana Levi has used 18 of her 20 vacation days.
public class LeaveRequestsApiTests : IClassFixture<TestDatabase>
{
    private readonly TestDatabase _database;

    public LeaveRequestsApiTests(TestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task Create_WithinBalance_Returns200WithRequest()
    {
        var client = _database.NewApiClient();
        var yossiId = await EmployeeIdAsync(client, "Yossi Cohen");

        var response = await client.PostAsJsonAsync("/api/leave-requests",
            new { employeeId = yossiId, type = 0, startDate = "2026-08-02", endDate = "2026-08-04" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<LeaveRequestDto>();
        Assert.Equal("Yossi Cohen", created!.EmployeeName);
        Assert.Equal(3, created.Days);
    }

    [Fact]
    public async Task Create_ExceedingBalance_Returns422ProblemDetails()
    {
        var client = _database.NewApiClient();
        var danaId = await EmployeeIdAsync(client, "Dana Levi");

        var response = await client.PostAsJsonAsync("/api/leave-requests",
            new { employeeId = danaId, type = 0, startDate = "2026-08-02", endDate = "2026-08-06" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("balance", problem!.Detail);
    }

    [Fact]
    public async Task Create_UnknownEmployee_Returns404()
    {
        var client = _database.NewApiClient();

        var response = await client.PostAsJsonAsync("/api/leave-requests",
            new { employeeId = 999, type = 0, startDate = "2026-08-02", endDate = "2026-08-04" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_EndDateBeforeStartDate_Returns400()
    {
        var client = _database.NewApiClient();

        var response = await client.PostAsJsonAsync("/api/leave-requests",
            new { employeeId = 1, type = 0, startDate = "2026-08-06", endDate = "2026-08-02" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutLeaveType_Returns400()
    {
        var client = _database.NewApiClient();

        var response = await client.PostAsJsonAsync("/api/leave-requests",
            new { employeeId = 1, startDate = "2026-08-02", endDate = "2026-08-04" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<int> EmployeeIdAsync(HttpClient client, string name)
    {
        var employees = await client.GetFromJsonAsync<List<EmployeeDto>>("/api/employees");
        return employees!.Single(e => e.Name == name).Id;
    }
}
