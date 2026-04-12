using System.Text;
using System.Text.RegularExpressions;
using DeskIQ.TicketSystem.Core.Entities;
using DeskIQ.TicketSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeskIQ.TicketSystem.API.Exceptions;
using DeskIQ.TicketSystem.API.Models;

namespace DeskIQ.TicketSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private static readonly Regex DepartmentCodePattern = new("^[A-Z0-9]{2,4}$", RegexOptions.Compiled);

    public DepartmentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Department>>> GetDepartments()
    {
        var departments = await _context.Departments
            .Include(d => d.Users.Where(u => u.IsActive))
            .Where(d => d.IsActive)
            .ToListAsync();

        return Ok(departments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Department>> GetDepartment(Guid id)
    {
        var department = await _context.Departments
            .Include(d => d.Users.Where(u => u.IsActive))
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
            return NotFound();

        return Ok(department);
    }

    [HttpGet("suggest-code")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> SuggestCode([FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BadRequestException(ErrorCodes.DEPARTMENT_CODE_REQUIRED, "Department name is required");

        var existingCodes = await _context.Departments
            .Select(d => d.Code)
            .ToListAsync();

        var suggested = BuildSuggestedCode(name, existingCodes);
        return Ok(new { code = suggested });
    }

    [HttpGet("code-availability")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> CheckCodeAvailability(
        [FromQuery] string code,
        [FromQuery] Guid? excludeDepartmentId = null)
    {
        var normalizedCode = NormalizeCode(code);
        if (!IsValidCode(normalizedCode))
            throw new BadRequestException(ErrorCodes.DEPARTMENT_CODE_INVALID, "Code must contain 2 to 4 uppercase letters/numbers.");

        var exists = await _context.Departments.AnyAsync(d =>
            d.Code == normalizedCode &&
            (!excludeDepartmentId.HasValue || d.Id != excludeDepartmentId.Value));

        return Ok(new { code = normalizedCode, available = !exists });
    }

    [HttpGet("management")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetDepartmentManagementView()
    {
        var managementData = await _context.Departments
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.Code,
                d.Description,
                d.AutoAssignRules,
                d.IsActive,
                d.CreatedAt,
                d.UpdatedAt,
                ActiveUsersCount = d.Users.Count(u => u.IsActive),
                InactiveUsersCount = d.Users.Count(u => !u.IsActive),
                OpenTicketsCount = d.Tickets.Count(t => t.Status == TicketStatus.Open),
                InProgressTicketsCount = d.Tickets.Count(t => t.Status == TicketStatus.InProgress),
                ClosedTicketsCount = d.Tickets.Count(t => t.Status == TicketStatus.Closed),
                TotalTicketsCount = d.Tickets.Count,
                EmailAccountsCount = d.EmailAccounts.Count,
                WhatsAppConfigsCount = d.WhatsAppConfigs.Count
            })
            .ToListAsync();

        return Ok(managementData);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Department>> CreateDepartment([FromBody] CreateDepartmentRequest request)
    {
        var normalizedCode = NormalizeCode(request.Code);
        if (!IsValidCode(normalizedCode))
            throw new BadRequestException(ErrorCodes.DEPARTMENT_CODE_INVALID, "Code must contain 2 to 4 uppercase letters/numbers.");

        var codeExists = await _context.Departments.AnyAsync(d => d.Code == normalizedCode);
        if (codeExists)
            throw new ConflictException(ErrorCodes.DEPARTMENT_CODE_EXISTS, "Department code is already in use.");

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Code = normalizedCode,
            Description = request.Description,
            AutoAssignRules = request.AutoAssignRules ?? "{}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, department);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] UpdateDepartmentRequest request)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var normalizedCode = NormalizeCode(request.Code);
            if (!string.Equals(normalizedCode, department.Code, StringComparison.Ordinal))
                throw new BadRequestException(ErrorCodes.DEPARTMENT_CODE_IMMUTABLE, "Department code is immutable and cannot be changed.");
        }

        department.Name = request.Name ?? department.Name;
        department.Description = request.Description ?? department.Description;
        department.AutoAssignRules = request.AutoAssignRules ?? department.AutoAssignRules;
        department.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDepartment(Guid id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null)
            return NotFound();

        department.IsActive = false;
        department.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static string NormalizeCode(string? code)
    {
        return (code ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static bool IsValidCode(string code)
    {
        return DepartmentCodePattern.IsMatch(code);
    }

    private static string BuildSuggestedCode(string name, IEnumerable<string> existingCodes)
    {
        var existingSet = existingCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToUpperInvariant())
            .ToHashSet(StringComparer.Ordinal);

        var cleanName = new string(name
            .Trim()
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());

        if (cleanName.Length == 0)
            cleanName = "GE";

        if (cleanName.Length == 1)
            cleanName = $"{cleanName}X";

        var maxLength = Math.Min(4, cleanName.Length);
        for (var length = 2; length <= maxLength; length++)
        {
            var candidate = cleanName[..length];
            if (!existingSet.Contains(candidate))
                return candidate;
        }

        var baseCode = cleanName[..Math.Min(3, cleanName.Length)];
        for (var i = 0; i <= 9; i++)
        {
            var suffix = i.ToString();
            var candidate = $"{baseCode[..Math.Min(4 - suffix.Length, baseCode.Length)]}{suffix}";
            if (candidate.Length >= 2 && candidate.Length <= 4 && !existingSet.Contains(candidate))
                return candidate;
        }

        var fallback = new StringBuilder(baseCode[..Math.Min(2, baseCode.Length)]);
        while (fallback.Length < 4)
        {
            fallback.Append('X');
        }

        return fallback.ToString();
    }
}

public class CreateDepartmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AutoAssignRules { get; set; }
}

public class UpdateDepartmentRequest
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public string? AutoAssignRules { get; set; }
}
