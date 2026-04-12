using DeskIQ.TicketSystem.Core.Entities;
using DeskIQ.TicketSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskIQ.TicketSystem.API.Services;

public class TicketIdGenerator : ITicketIdGenerator
{
    private readonly AppDbContext _context;

    public TicketIdGenerator(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateNextAsync(Guid departmentId, string departmentCode, DateTime createdAtUtc)
    {
        var normalizedCode = departmentCode.Trim().ToUpperInvariant();
        var year = createdAtUtc.Year;
        var shortYear = year % 100;

        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        var sequence = await _context.TicketSequences
            .FirstOrDefaultAsync(s => s.DepartmentId == departmentId && s.Year == year);

        if (sequence is null)
        {
            sequence = new TicketSequence
            {
                Id = Guid.NewGuid(),
                DepartmentId = departmentId,
                Year = year,
                LastValue = 1,
                UpdatedAt = createdAtUtc
            };

            _context.TicketSequences.Add(sequence);
        }
        else
        {
            sequence.LastValue += 1;
            sequence.UpdatedAt = createdAtUtc;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return $"{normalizedCode}-{shortYear:D2}-{sequence.LastValue:D6}";
    }
}