using System.ComponentModel.DataAnnotations;
using LeaveManagement.Api.Models;

namespace LeaveManagement.Api.Contracts;

public class CreateLeaveRequestDto : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public required int EmployeeId { get; set; }

    [EnumDataType(typeof(LeaveType))]
    public required LeaveType Type { get; set; }

    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate < StartDate)
            yield return new ValidationResult("End date must not be before start date.", [nameof(EndDate)]);
    }
}
