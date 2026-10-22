using LeaveManagement.Api.Contracts;
using LeaveManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/leave-requests")]
public class LeaveRequestsController : ControllerBase
{
    private readonly LeaveRequestService _service;

    public LeaveRequestsController(LeaveRequestService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string name, CancellationToken cancellationToken) =>
        Ok(await _service.SearchByEmployeeNameAsync(name, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreateLeaveRequestDto dto, CancellationToken cancellationToken) =>
        Ok(await _service.CreateAsync(dto, cancellationToken));
}
