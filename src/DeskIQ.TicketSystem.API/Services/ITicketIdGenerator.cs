namespace DeskIQ.TicketSystem.API.Services;

public interface ITicketIdGenerator
{
    Task<string> GenerateNextAsync(Guid departmentId, string departmentCode, DateTime createdAtUtc);
}