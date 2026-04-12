using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using DeskIQ.TicketSystem.API.Configuration;
using DeskIQ.TicketSystem.Infrastructure.Data;

namespace DeskIQ.TicketSystem.API.Services;

public class FileStorageService : IFileStorageService
{
    private readonly FileStorageSettings _settings;
    private readonly AppDbContext _context;

    public FileStorageService(
        IOptions<FileStorageSettings> settings,
        AppDbContext context)
    {
        _settings = settings.Value;
        _context = context;

        // Ensure BasePath is absolute and exists
        if (!Path.IsPathRooted(_settings.BasePath))
        {
            var basePath = Path.GetFullPath(_settings.BasePath);
            _settings.BasePath = basePath;
        }

        // Create base directory if it doesn't exist
        Directory.CreateDirectory(_settings.BasePath);
    }

    public async Task<string> SaveAttachmentAsync(Guid ticketId, IFormFile file, Guid userId)
    {
        // Create directory if it doesn't exist
        var ticketPath = Path.Combine(_settings.BasePath, _settings.TicketsPath, ticketId.ToString());
        Directory.CreateDirectory(ticketPath);

        // Generate unique filename
        var fileExtension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(ticketPath, uniqueFileName);

        // Save file to disk
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return relative path for storage in database
        var relativePath = Path.Combine(_settings.TicketsPath, ticketId.ToString(), uniqueFileName);
        return relativePath.Replace("\\", "/");
    }

    public async Task<string?> GetAttachmentPathAsync(Guid attachmentId)
    {
        var attachment = await _context.TicketAttachments.FindAsync(attachmentId);
        if (attachment == null)
            return null;

        return attachment.FilePath;
    }

    public async Task<bool> DeleteAttachmentAsync(Guid attachmentId)
    {
        var attachment = await _context.TicketAttachments.FindAsync(attachmentId);
        if (attachment == null)
            return false;

        var fullPath = GetFullFilePath(attachment.FilePath);

        // Delete file from disk if it exists
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return true;
    }

    public string GetFullFilePath(string relativePath)
    {
        // Normalize path separators to match the OS
        var normalizedPath = relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()).Replace("\\", Path.DirectorySeparatorChar.ToString());
        return Path.Combine(_settings.BasePath, normalizedPath);
    }
}
