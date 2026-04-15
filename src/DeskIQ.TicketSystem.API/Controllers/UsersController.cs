using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DeskIQ.TicketSystem.Infrastructure.Data;
using DeskIQ.TicketSystem.Core.Entities;
using DeskIQ.TicketSystem.API.Exceptions;
using DeskIQ.TicketSystem.API.Models;
using DeskIQ.TicketSystem.Application.Services;

namespace DeskIQ.TicketSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuditService _auditService;
    private readonly PermissionService _permissionService;

    public UsersController(AppDbContext context, AuditService auditService, PermissionService permissionService)
    {
        _context = context;
        _auditService = auditService;
        _permissionService = permissionService;
    }

    [HttpGet]
    [Authorize(Roles = "Administrador,OperadorSupervisor,SupervisorGeneral,Auditor")]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] bool? isActive = null)
    {
        var query = _context.Users
            .Include(u => u.Department)
            .AsQueryable();

        if (departmentId.HasValue)
            query = query.Where(u => u.DepartmentId == departmentId.Value);

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var users = await query
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                DepartmentId = u.DepartmentId,
                DepartmentName = u.Department != null ? u.Department.Name : null,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                ExtId = u.ExtId,
                DepartmentPendingAssign = u.DepartmentPendingAssign
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("assignees")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAssignees(
        [FromQuery] Guid departmentId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            var currentUser = await _context.Users.FindAsync(currentUserId.Value);
            if (currentUser == null)
                return Unauthorized();

            // Validate user can view assignees in this department
            if (!_permissionService.CanViewAssignees(currentUser, departmentId))
                return Forbid();

            var query = _context.Users
                .Include(u => u.Department)
                .Where(u => u.DepartmentId == departmentId && u.IsActive == true)
                .AsQueryable();

            var assignees = await query
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    DepartmentId = u.DepartmentId,
                    DepartmentName = u.Department != null ? u.Department.Name : null,
                    Role = u.Role,
                    IsActive = u.IsActive,
                })
                .ToListAsync();

            return Ok(assignees);
        }
        catch (Exception ex)
        {
            // Log the full exception for debugging
            Console.WriteLine($"Error in GetAssignees: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Administrador,OperadorSupervisor,SupervisorGeneral,Auditor")]
    public async Task<ActionResult<UserResponseDto>> GetUser(Guid id)
    {
        var user = await _context.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound();

        return Ok(MapToDto(user));
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        // Check if email already exists
        var existingUser = await _context.Users
            .AnyAsync(u => u.Email == request.Email);

        if (existingUser)
            throw new BadRequestException(ErrorCodes.EMAIL_ALREADY_EXISTS, "El correo electrónico ya existe");

        // Check if ExtId already exists (for SSO users)
        if (!string.IsNullOrWhiteSpace(request.ExtId))
        {
            var existingExtId = await _context.Users
                .AnyAsync(u => u.ExtId == request.ExtId);
            if (existingExtId)
                throw new BadRequestException(ErrorCodes.EMAIL_ALREADY_EXISTS, "El ID externo ya existe");
        }

        // Validate department exists
        var department = await _context.Departments.FindAsync(request.DepartmentId);
        if (department == null)
            throw new BadRequestException(ErrorCodes.DEPARTMENT_NOT_FOUND, "Departamento no encontrado");

        // Password is required for non-SSO users
        if (string.IsNullOrWhiteSpace(request.Password) && string.IsNullOrWhiteSpace(request.ExtId))
            throw new BadRequestException(ErrorCodes.VALIDATION_ERROR, "Se requiere contraseña para usuarios que no usan SSO");

        var currentUserId = GetCurrentUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PasswordHash = !string.IsNullOrWhiteSpace(request.Password)
                ? BCrypt.Net.BCrypt.HashPassword(request.Password)
                : string.Empty, // Empty for SSO users
            DepartmentId = request.DepartmentId,
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ExtId = request.ExtId,
            DepartmentPendingAssign = request.DepartmentPendingAssign
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Log user creation
        await _auditService.LogUserCreationAsync(user.Id, user.Role, user.DepartmentId, currentUserId.Value, ipAddress);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, MapToDto(user));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        var currentUserId = GetCurrentUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Track role changes for audit
        var oldRole = user.Role;
        var oldDepartmentId = user.DepartmentId;

        user.Name = request.Name ?? user.Name;
        user.Email = request.Email ?? user.Email;
        user.DepartmentId = request.DepartmentId ?? user.DepartmentId;
        user.Role = request.Role ?? user.Role;
        user.IsActive = request.IsActive ?? user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        await _context.SaveChangesAsync();

        // Log role change if it occurred
        if (request.Role.HasValue && request.Role.Value != oldRole)
        {
            await _auditService.LogRoleChangeAsync(user.Id, oldRole, request.Role.Value, currentUserId.Value, ipAddress);
        }

        // Log department change if it occurred
        if (request.DepartmentId.HasValue && request.DepartmentId.Value != oldDepartmentId)
        {
            await _auditService.LogDepartmentAssignmentAsync(user.Id, oldDepartmentId, request.DepartmentId.Value, currentUserId.Value, ipAddress);
        }

        // Log user update
        await _auditService.LogUserUpdateAsync(user.Id, currentUserId.Value, ipAddress);

        return NoContent();
    }

    private static UserResponseDto MapToDto(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department?.Name,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            ExtId = user.ExtId,
            DepartmentPendingAssign = user.DepartmentPendingAssign
        };
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }
}

public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; } // Optional for SSO users
    public Guid DepartmentId { get; set; }
    public UserRole Role { get; set; }
    public string? ExtId { get; set; } // External ID from SSO provider
    public bool DepartmentPendingAssign { get; set; } // Flag for SSO users without department
}

public class UpdateUserRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public Guid? DepartmentId { get; set; }
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
}

public class UserResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? ExtId { get; set; }
    public bool DepartmentPendingAssign { get; set; }
}
