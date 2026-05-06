using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.Constants;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Organization;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize(Roles = SystemRoles.Administrator)]
[Route("api/admin/organization")]
public class AdminOrganizationController : ControllerBase
{
    private readonly IOrganizationSetupService _organizationSetupService;

    public AdminOrganizationController(IOrganizationSetupService organizationSetupService)
    {
        _organizationSetupService = organizationSetupService;
    }

    [HttpGet("summary")]
    [ProducesResponseType<OrganizationSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        return Ok(await _organizationSetupService.GetSummaryAsync(cancellationToken));
    }

    [HttpGet("options")]
    [ProducesResponseType<OrganizationOptionsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationOptionsDto>> GetOptions(CancellationToken cancellationToken)
    {
        return Ok(await _organizationSetupService.GetOptionsAsync(cancellationToken));
    }

    [HttpGet("departments")]
    [ProducesResponseType<PagedResultDto<OrganizationRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<OrganizationRecordDto>>> GetDepartments([FromQuery] OrganizationListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _organizationSetupService.GetDepartmentsAsync(query, cancellationToken));
    }

    [HttpGet("departments/{departmentId:guid}")]
    [ProducesResponseType<OrganizationRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationRecordDto>> GetDepartmentById(Guid departmentId, CancellationToken cancellationToken)
    {
        return Ok(await _organizationSetupService.GetDepartmentByIdAsync(departmentId, cancellationToken));
    }

    [HttpPost("departments")]
    [ProducesResponseType<OrganizationRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<OrganizationRecordDto>> CreateDepartment([FromBody] SaveDepartmentRequestDto request, CancellationToken cancellationToken)
    {
        var department = await _organizationSetupService.CreateDepartmentAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetDepartmentById), new { departmentId = department.Id }, department);
    }

    [HttpPut("departments/{departmentId:guid}")]
    [ProducesResponseType<OrganizationRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationRecordDto>> UpdateDepartment(Guid departmentId, [FromBody] SaveDepartmentRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _organizationSetupService.UpdateDepartmentAsync(departmentId, request, cancellationToken));
    }

    [HttpDelete("departments/{departmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDepartment(Guid departmentId, CancellationToken cancellationToken)
    {
        await _organizationSetupService.DeleteDepartmentAsync(departmentId, cancellationToken);
        return NoContent();
    }

    [HttpGet("positions")]
    [ProducesResponseType<PagedResultDto<OrganizationRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<OrganizationRecordDto>>> GetPositions([FromQuery] OrganizationListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _organizationSetupService.GetPositionsAsync(query, cancellationToken));
    }

    [HttpGet("positions/{positionId:guid}")]
    [ProducesResponseType<OrganizationRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationRecordDto>> GetPositionById(Guid positionId, CancellationToken cancellationToken)
    {
        return Ok(await _organizationSetupService.GetPositionByIdAsync(positionId, cancellationToken));
    }

    [HttpPost("positions")]
    [ProducesResponseType<OrganizationRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<OrganizationRecordDto>> CreatePosition([FromBody] SavePositionRequestDto request, CancellationToken cancellationToken)
    {
        var position = await _organizationSetupService.CreatePositionAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPositionById), new { positionId = position.Id }, position);
    }

    [HttpPut("positions/{positionId:guid}")]
    [ProducesResponseType<OrganizationRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationRecordDto>> UpdatePosition(Guid positionId, [FromBody] SavePositionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _organizationSetupService.UpdatePositionAsync(positionId, request, cancellationToken));
    }

    [HttpDelete("positions/{positionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePosition(Guid positionId, CancellationToken cancellationToken)
    {
        await _organizationSetupService.DeletePositionAsync(positionId, cancellationToken);
        return NoContent();
    }

    [HttpGet("branches")]
    [ProducesResponseType<PagedResultDto<OrganizationRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<OrganizationRecordDto>>> GetBranches([FromQuery] OrganizationListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _organizationSetupService.GetBranchesAsync(query, cancellationToken));
    }

    [HttpGet("branches/{branchId:guid}")]
    [ProducesResponseType<OrganizationRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationRecordDto>> GetBranchById(Guid branchId, CancellationToken cancellationToken)
    {
        return Ok(await _organizationSetupService.GetBranchByIdAsync(branchId, cancellationToken));
    }

    [HttpPost("branches")]
    [ProducesResponseType<OrganizationRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<OrganizationRecordDto>> CreateBranch([FromBody] SaveBranchRequestDto request, CancellationToken cancellationToken)
    {
        var branch = await _organizationSetupService.CreateBranchAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBranchById), new { branchId = branch.Id }, branch);
    }

    [HttpPut("branches/{branchId:guid}")]
    [ProducesResponseType<OrganizationRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationRecordDto>> UpdateBranch(Guid branchId, [FromBody] SaveBranchRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _organizationSetupService.UpdateBranchAsync(branchId, request, cancellationToken));
    }

    [HttpDelete("branches/{branchId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteBranch(Guid branchId, CancellationToken cancellationToken)
    {
        await _organizationSetupService.DeleteBranchAsync(branchId, cancellationToken);
        return NoContent();
    }

    [HttpGet("employment-types")]
    [ProducesResponseType<PagedResultDto<OrganizationRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<OrganizationRecordDto>>> GetEmploymentTypes([FromQuery] OrganizationListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _organizationSetupService.GetEmploymentTypesAsync(query, cancellationToken));
    }

    [HttpGet("employment-types/{employmentTypeId:guid}")]
    [ProducesResponseType<OrganizationRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationRecordDto>> GetEmploymentTypeById(Guid employmentTypeId, CancellationToken cancellationToken)
    {
        return Ok(await _organizationSetupService.GetEmploymentTypeByIdAsync(employmentTypeId, cancellationToken));
    }

    [HttpPost("employment-types")]
    [ProducesResponseType<OrganizationRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<OrganizationRecordDto>> CreateEmploymentType([FromBody] SaveEmploymentTypeRequestDto request, CancellationToken cancellationToken)
    {
        var employmentType = await _organizationSetupService.CreateEmploymentTypeAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetEmploymentTypeById), new { employmentTypeId = employmentType.Id }, employmentType);
    }

    [HttpPut("employment-types/{employmentTypeId:guid}")]
    [ProducesResponseType<OrganizationRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationRecordDto>> UpdateEmploymentType(Guid employmentTypeId, [FromBody] SaveEmploymentTypeRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _organizationSetupService.UpdateEmploymentTypeAsync(employmentTypeId, request, cancellationToken));
    }

    [HttpDelete("employment-types/{employmentTypeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteEmploymentType(Guid employmentTypeId, CancellationToken cancellationToken)
    {
        await _organizationSetupService.DeleteEmploymentTypeAsync(employmentTypeId, cancellationToken);
        return NoContent();
    }

    [HttpGet("employment-statuses")]
    [ProducesResponseType<PagedResultDto<OrganizationRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<OrganizationRecordDto>>> GetEmploymentStatuses([FromQuery] OrganizationListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _organizationSetupService.GetEmploymentStatusesAsync(query, cancellationToken));
    }

    [HttpGet("employment-statuses/{employmentStatusId:guid}")]
    [ProducesResponseType<OrganizationRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationRecordDto>> GetEmploymentStatusById(Guid employmentStatusId, CancellationToken cancellationToken)
    {
        return Ok(await _organizationSetupService.GetEmploymentStatusByIdAsync(employmentStatusId, cancellationToken));
    }

    [HttpPost("employment-statuses")]
    [ProducesResponseType<OrganizationRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<OrganizationRecordDto>> CreateEmploymentStatus([FromBody] SaveEmploymentStatusRequestDto request, CancellationToken cancellationToken)
    {
        var employmentStatus = await _organizationSetupService.CreateEmploymentStatusAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetEmploymentStatusById), new { employmentStatusId = employmentStatus.Id }, employmentStatus);
    }

    [HttpPut("employment-statuses/{employmentStatusId:guid}")]
    [ProducesResponseType<OrganizationRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationRecordDto>> UpdateEmploymentStatus(Guid employmentStatusId, [FromBody] SaveEmploymentStatusRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _organizationSetupService.UpdateEmploymentStatusAsync(employmentStatusId, request, cancellationToken));
    }

    [HttpDelete("employment-statuses/{employmentStatusId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteEmploymentStatus(Guid employmentStatusId, CancellationToken cancellationToken)
    {
        await _organizationSetupService.DeleteEmploymentStatusAsync(employmentStatusId, cancellationToken);
        return NoContent();
    }
}
