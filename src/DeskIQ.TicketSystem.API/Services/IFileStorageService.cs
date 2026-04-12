using Microsoft.AspNetCore.Http;

namespace DeskIQ.TicketSystem.API.Services;

public interface IFileStorageService
{
    Task<string> SaveAttachmentAsync(Guid ticketId, IFormFile file, Guid userId);
    Task<string?> GetAttachmentPathAsync(Guid attachmentId);
    Task<bool> DeleteAttachmentAsync(Guid attachmentId);
    string GetFullFilePath(string relativePath);
}
