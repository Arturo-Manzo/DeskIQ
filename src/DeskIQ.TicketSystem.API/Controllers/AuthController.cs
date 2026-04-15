using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DeskIQ.TicketSystem.Infrastructure.Data;
using DeskIQ.TicketSystem.Core.Entities;
using DeskIQ.TicketSystem.API.Services;
using DeskIQ.TicketSystem.API.Exceptions;
using DeskIQ.TicketSystem.API.Models;

namespace DeskIQ.TicketSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;

    public AuthController(AppDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !user.IsActive)
            throw new UnauthorizedException(ErrorCodes.INVALID_CREDENTIALS, "Credenciales inválidas");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException(ErrorCodes.INVALID_CREDENTIALS, "Credenciales inválidas");

        var token = _jwtService.GenerateToken(user);

        // Remove sensitive information
        user.PasswordHash = string.Empty;

        return Ok(new LoginResponse
        {
            Token = token,
            User = new AuthUserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                DepartmentId = user.DepartmentId,
                Department = user.Department == null
                    ? null
                    : new DepartmentDto
                    {
                        Id = user.Department.Id,
                        Name = user.Department.Name,
                        Description = user.Department.Description
                    }
            }
        });
    }

    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
    {
        // Check if email already exists
        var existingUser = await _context.Users
            .AnyAsync(u => u.Email == request.Email);

        if (existingUser)
            throw new BadRequestException(ErrorCodes.EMAIL_ALREADY_EXISTS, "El correo electrónico ya existe");

        // Verify department exists
        var department = await _context.Departments.FindAsync(request.DepartmentId);
        if (department == null)
            throw new BadRequestException(ErrorCodes.INVALID_DEPARTMENT, "Departamento inválido");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DepartmentId = request.DepartmentId,
            Role = UserRole.Cliente, // Default role for new users
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Reload with department
        user = await _context.Users
            .Include(u => u.Department)
            .FirstAsync(u => u.Id == user.Id);

        var token = _jwtService.GenerateToken(user);

        // Remove sensitive information
        user.PasswordHash = string.Empty;

        return Ok(new LoginResponse
        {
            Token = token,
            User = new AuthUserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                DepartmentId = user.DepartmentId,
                Department = user.Department == null
                    ? null
                    : new DepartmentDto
                    {
                        Id = user.Department.Id,
                        Name = user.Department.Name,
                        Description = user.Department.Description
                    }
            }
        });
    }

    [HttpPost("validate")]
    public ActionResult<UserInfo> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        var principal = _jwtService.ValidateToken(request.Token);
        if (principal == null)
            throw new UnauthorizedException(ErrorCodes.INVALID_TOKEN, "Token inválido");

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        var name = principal.FindFirst(ClaimTypes.Name)?.Value;
        var role = principal.FindFirst(ClaimTypes.Role)?.Value;
        var departmentId = principal.FindFirst("departmentId")?.Value;

        return Ok(new UserInfo
        {
            Id = Guid.Parse(userId ?? string.Empty),
            Email = email ?? string.Empty,
            Name = name ?? string.Empty,
            Role = Enum.Parse<UserRole>(role ?? "Agent"),
            DepartmentId = Guid.Parse(departmentId ?? string.Empty)
        });
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public AuthUserDto User { get; set; } = null!;
}

public class AuthUserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid DepartmentId { get; set; }
    public DepartmentDto? Department { get; set; }
}

public class DepartmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ValidateTokenRequest
{
    public string Token { get; set; } = string.Empty;
}

public class UserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid DepartmentId { get; set; }
}
