using DeskIQ.TicketSystem.Core.Entities;
using FluentValidation;

namespace DeskIQ.TicketSystem.Application.Validators;

public class CreateSubticketRequestValidator : AbstractValidator<CreateSubticketRequest>
{
    public CreateSubticketRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value");

        RuleFor(x => x.BlockedReason)
            .NotEmpty().WithMessage("Blocked reason is required when subticket is blocked")
            .When(x => x.IsBlocked && string.IsNullOrWhiteSpace(x.BlockedReason));
    }
}

public class CreateSubticketRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; }
    public Guid? AssignedToId { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockedReason { get; set; }
}
