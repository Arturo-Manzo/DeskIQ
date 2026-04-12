using DeskIQ.TicketSystem.Core.Entities;
using FluentValidation;

namespace DeskIQ.TicketSystem.Application.Validators;

public class CreateTicketRequestValidator : AbstractValidator<CreateTicketRequest>
{
    public CreateTicketRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required");

        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("DepartmentId is required");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value");

        RuleFor(x => x.Source)
            .IsInEnum().WithMessage("Invalid source value");
    }
}

public class CreateTicketRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; }
    public Guid DepartmentId { get; set; }
    public TicketSource Source { get; set; }
    public string? ExternalId { get; set; }
}
