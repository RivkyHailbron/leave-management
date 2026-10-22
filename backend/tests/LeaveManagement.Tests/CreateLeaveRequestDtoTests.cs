using System.ComponentModel.DataAnnotations;
using LeaveManagement.Api.Models;
using Xunit;

namespace LeaveManagement.Tests;

public class CreateLeaveRequestDtoTests
{
    [Fact]
    public void Valid_Request_PassesValidation()
    {
        var errors = Validate(NewDto());

        Assert.Empty(errors);
    }

    [Fact]
    public void EndDateBeforeStartDate_IsInvalid()
    {
        var errors = Validate(NewDto(start: new DateTime(2026, 8, 6), end: new DateTime(2026, 8, 2)));

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateLeaveRequestDto.EndDate)));
    }

    [Fact]
    public void UndefinedLeaveType_IsInvalid()
    {
        var errors = Validate(NewDto(type: (LeaveType)99));

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateLeaveRequestDto.Type)));
    }

    [Fact]
    public void NonPositiveEmployeeId_IsInvalid()
    {
        var errors = Validate(NewDto(employeeId: 0));

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateLeaveRequestDto.EmployeeId)));
    }

    private static CreateLeaveRequestDto NewDto(
        int employeeId = 1,
        LeaveType type = LeaveType.Vacation,
        DateTime? start = null,
        DateTime? end = null) => new()
    {
        EmployeeId = employeeId,
        Type = type,
        StartDate = start ?? new DateTime(2026, 8, 2),
        EndDate = end ?? new DateTime(2026, 8, 4)
    };

    private static List<ValidationResult> Validate(CreateLeaveRequestDto dto)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), errors, validateAllProperties: true);
        return errors;
    }
}
