using DeskIQ.TicketSystem.Core.Entities;
using FluentValidation;

namespace DeskIQ.TicketSystem.Application.Validators;

public class UpdateTicketRequestValidator : AbstractValidator<UpdateTicketRequest>
{
    public UpdateTicketRequestValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters")
            .When(x => x.Title != null);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status value")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value")
            .When(x => x.Priority.HasValue);

        RuleFor(x => x.BlockedReason)
            .NotEmpty().WithMessage("Blocked reason is required when ticket is blocked")
            .When(x => x.IsBlocked == true && string.IsNullOrWhiteSpace(x.BlockedReason));
    }
}

public class UpdateTicketRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public TicketStatus? Status { get; set; }
    public TicketPriority? Priority { get; set; }
    public Guid? AssignedToId { get; set; }
    public bool? IsBlocked { get; set; }
    public string? BlockedReason { get; set; }
    public bool CloseOpenSubticketsWithParent { get; set; }
}
